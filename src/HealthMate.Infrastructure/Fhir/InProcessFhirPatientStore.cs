using HealthMate.Fhir.Ports;
using HealthMate.Fhir.Ports.Dtos;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Fhir;

public sealed class InProcessFhirPatientStore(
    HealthMateContext context,
    TimeProvider clock) : IFhirPatientStore
{
    private const string FhirPlaceholderNationalIdImage = "fhir_patient_national_id.png";

    public async Task<FhirPatientSnapshot?> ReadAsync(string fhirId, CancellationToken ct)
    {
        var patient = await context.Patients
            .AsNoTracking()
            .Include(p => p.ApplicationUser)
            .SingleOrDefaultAsync(p => p.Patient_Fhir_Id == fhirId, ct);

        return patient is null ? null : ToSnapshot(patient);
    }

    public async Task<FhirPatientSearchResult> SearchAsync(FhirPatientSearchQuery query, CancellationToken ct)
    {
        var patients = context.Patients
            .AsNoTracking()
            .Include(p => p.ApplicationUser)
            .Where(static p => !p.IsDeleted);

        if (query.Ids.Count > 0)
        {
            patients = patients.Where(p => query.Ids.Contains(p.Patient_Fhir_Id));
        }

        foreach (var filter in query.LastUpdated)
        {
            patients = ApplyDateTimeFilter(patients, filter);
        }

        if (query.Name is not null)
        {
            var lowered = query.Name.Value.ToLowerInvariant();
            patients = query.Name.Exact
                ? patients.Where(p => p.ApplicationUser != null
                    && (p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name).ToLower() == lowered)
                : patients.Where(p => p.ApplicationUser != null
                    && (p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name).ToLower().Contains(lowered));
        }

        if (query.Identifier is not null)
        {
            patients = patients.Where(p => p.NationalId == query.Identifier.Value);
        }

        foreach (var filter in query.BirthDate)
        {
            patients = ApplyDateFilter(patients, filter);
        }

        if (!string.IsNullOrWhiteSpace(query.Gender))
        {
            if (TryParseGender(query.Gender, out var gender))
            {
                patients = patients.Where(p => p.Gender == gender);
            }
            else
            {
                patients = patients.Where(static p => false);
            }
        }

        var total = await patients.CountAsync(ct);
        var sorted = ApplySort(patients, query.Sorts);
        var page = await sorted
            .Skip(query.Offset)
            .Take(query.Count)
            .ToListAsync(ct);

        return new FhirPatientSearchResult(page.Select(ToSnapshot).ToArray(), total, query.Offset, query.Count);
    }

    public async Task<FhirPatientSnapshot> CreateAsync(FhirPatientSnapshot snapshot, CancellationToken ct)
    {
        var patient = new Patient
        {
            Patient_Fhir_Id = string.IsNullOrWhiteSpace(snapshot.FhirId) ? Guid.NewGuid().ToString() : snapshot.FhirId,
            NationalId = snapshot.NationalId,
            NationalIdImageUrl = FhirPlaceholderNationalIdImage,
            BirthDate = snapshot.BirthDate,
            Gender = ToGender(snapshot.Gender),
            Governorate = snapshot.Governorate,
            City = snapshot.City,
            // IsVerified is admin-managed; FHIR-created patients land unverified until an admin reviews them.
            IsDeleted = snapshot.IsDeleted,
            DeletedAt = snapshot.IsDeleted ? clock.GetUtcNow() : null,
            RowVersion = 1
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync(ct);

        return (await ReadAsync(patient.Patient_Fhir_Id, ct))!;
    }

    public async Task<FhirPatientSnapshot> UpdateAsync(FhirPatientSnapshot snapshot, uint expectedVersion, CancellationToken ct)
    {
        var patient = await context.Patients
            .Include(p => p.ApplicationUser)
            .SingleOrDefaultAsync(p => p.Patient_Fhir_Id == snapshot.FhirId, ct);

        if (patient is null)
        {
            throw new FhirNotFoundException("Patient", snapshot.FhirId);
        }

        if (patient.RowVersion != expectedVersion)
        {
            throw new FhirConcurrencyException("Patient", snapshot.FhirId);
        }

        patient.NationalId = snapshot.NationalId;
        patient.BirthDate = snapshot.BirthDate;
        patient.Gender = ToGender(snapshot.Gender);
        patient.Governorate = snapshot.Governorate;
        patient.City = snapshot.City;
        // IsVerified is admin-managed and intentionally not touched here; FHIR PUT must not flip verification status.
        patient.IsDeleted = snapshot.IsDeleted;
        patient.DeletedAt = snapshot.IsDeleted ? patient.DeletedAt ?? clock.GetUtcNow() : null;

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new FhirConcurrencyException("Patient", snapshot.FhirId);
        }

        return (await ReadAsync(snapshot.FhirId, ct))!;
    }

    public async Task DeleteAsync(string fhirId, uint? expectedVersion, CancellationToken ct)
    {
        var patient = await context.Patients.SingleOrDefaultAsync(p => p.Patient_Fhir_Id == fhirId, ct);
        if (patient is null)
        {
            throw new FhirNotFoundException("Patient", fhirId);
        }

        if (expectedVersion.HasValue && patient.RowVersion != expectedVersion.Value)
        {
            throw new FhirConcurrencyException("Patient", fhirId);
        }

        if (patient.IsDeleted)
        {
            return;
        }

        patient.IsDeleted = true;
        patient.DeletedAt = clock.GetUtcNow();

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new FhirConcurrencyException("Patient", fhirId);
        }
    }

    public async Task<FhirPatientHistoryResult> ReadHistoryAsync(
        string fhirId,
        int count,
        DateTimeOffset? since,
        CancellationToken ct)
    {
        var history = context.PatientHistories.AsNoTracking().Where(h => h.Patient_Fhir_Id == fhirId);
        if (since.HasValue)
        {
            history = history.Where(h => h.RecordedAt >= since.Value);
        }

        var total = await history.CountAsync(ct);
        var rows = await history
            .OrderByDescending(h => h.RecordedAt)
            .ThenByDescending(h => h.RowVersion)
            .Take(count)
            .ToListAsync(ct);

        return new FhirPatientHistoryResult(rows.Select(ToHistoryEntry).ToArray(), total, count);
    }

    public async Task<FhirPatientHistoryEntry?> ReadVersionAsync(string fhirId, uint versionId, CancellationToken ct)
    {
        var row = await context.PatientHistories
            .AsNoTracking()
            .Where(h => h.Patient_Fhir_Id == fhirId && h.RowVersion == versionId)
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToHistoryEntry(row);
    }

    private static IQueryable<Patient> ApplyDateTimeFilter(IQueryable<Patient> query, FhirDateTimeFilter filter)
    {
        return filter.Prefix switch
        {
            FhirSearchPrefix.Gt => query.Where(p => p.LastUpdated > filter.Value),
            FhirSearchPrefix.Lt => query.Where(p => p.LastUpdated < filter.Value),
            FhirSearchPrefix.Ge => query.Where(p => p.LastUpdated >= filter.Value),
            FhirSearchPrefix.Le => query.Where(p => p.LastUpdated <= filter.Value),
            _ => query.Where(p => p.LastUpdated == filter.Value)
        };
    }

    private static IQueryable<Patient> ApplyDateFilter(IQueryable<Patient> query, FhirDateFilter filter)
    {
        return filter.Prefix switch
        {
            FhirSearchPrefix.Gt => query.Where(p => p.BirthDate > filter.Value),
            FhirSearchPrefix.Lt => query.Where(p => p.BirthDate < filter.Value),
            FhirSearchPrefix.Ge => query.Where(p => p.BirthDate >= filter.Value),
            FhirSearchPrefix.Le => query.Where(p => p.BirthDate <= filter.Value),
            _ => query.Where(p => p.BirthDate == filter.Value)
        };
    }

    private static IOrderedQueryable<Patient> ApplySort(IQueryable<Patient> query, IReadOnlyList<FhirSort> sorts)
    {
        IOrderedQueryable<Patient>? ordered = null;
        foreach (var sort in sorts.Count == 0 ? FhirPatientSearchQuery.Empty.Sorts : sorts)
        {
            ordered = ApplySortKey(ordered ?? query, ordered is not null, sort);
        }

        return ordered ?? query.OrderByDescending(p => p.LastUpdated);
    }

    private static IOrderedQueryable<Patient> ApplySortKey(IQueryable<Patient> query, bool thenBy, FhirSort sort)
    {
        var ordered = query as IOrderedQueryable<Patient>;
        return sort.Field switch
        {
            "name" when sort.Descending && thenBy => ordered!.ThenByDescending(p => p.ApplicationUser == null ? "" : p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name),
            "name" when sort.Descending => query.OrderByDescending(p => p.ApplicationUser == null ? "" : p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name),
            "name" when thenBy => ordered!.ThenBy(p => p.ApplicationUser == null ? "" : p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name),
            "name" => query.OrderBy(p => p.ApplicationUser == null ? "" : p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name),
            "birthdate" when sort.Descending && thenBy => ordered!.ThenByDescending(p => p.BirthDate),
            "birthdate" when sort.Descending => query.OrderByDescending(p => p.BirthDate),
            "birthdate" when thenBy => ordered!.ThenBy(p => p.BirthDate),
            "birthdate" => query.OrderBy(p => p.BirthDate),
            _ when sort.Descending && thenBy => ordered!.ThenByDescending(p => p.LastUpdated),
            _ when sort.Descending => query.OrderByDescending(p => p.LastUpdated),
            _ when thenBy => ordered!.ThenBy(p => p.LastUpdated),
            _ => query.OrderBy(p => p.LastUpdated)
        };
    }

    private static FhirPatientSnapshot ToSnapshot(Patient patient)
    {
        return new FhirPatientSnapshot(
            patient.Patient_Fhir_Id,
            patient.NationalId,
            patient.ApplicationUser is null ? null : $"{patient.ApplicationUser.First_Name} {patient.ApplicationUser.Last_Name}".Trim(),
            patient.BirthDate,
            FromGender(patient.Gender),
            patient.Governorate,
            patient.City,
            patient.ApplicationUser?.PhoneNumber,
            patient.ApplicationUser?.Email,
            patient.IsVerified,
            patient.LastUpdated,
            patient.RowVersion,
            patient.IsDeleted);
    }

    private static FhirPatientHistoryEntry ToHistoryEntry(PatientHistory history)
    {
        var snapshot = new FhirPatientSnapshot(
            history.Patient_Fhir_Id,
            history.NationalId,
            history.Name,
            history.BirthDate,
            FromGender(history.Gender),
            history.Governorate,
            history.City,
            history.PhoneE164,
            history.Email,
            history.IsVerified,
            history.LastUpdated,
            history.RowVersion,
            history.IsDeleted);

        return new FhirPatientHistoryEntry(snapshot, ToFhirOperation(history.OperationType), history.RecordedAt);
    }

    private static Gender ToGender(string gender)
    {
        return gender.Equals("female", StringComparison.OrdinalIgnoreCase)
            ? Gender.Female
            : Gender.Male;
    }

    private static bool TryParseGender(string gender, out Gender parsed)
    {
        if (gender.Equals("male", StringComparison.OrdinalIgnoreCase))
        {
            parsed = Gender.Male;
            return true;
        }

        if (gender.Equals("female", StringComparison.OrdinalIgnoreCase))
        {
            parsed = Gender.Female;
            return true;
        }

        parsed = default;
        return false;
    }

    private static string FromGender(Gender gender)
    {
        return gender == Gender.Female ? "female" : "male";
    }

    private static FhirHistoryOperation ToFhirOperation(PatientHistoryOperation operation)
    {
        return operation switch
        {
            PatientHistoryOperation.Create => FhirHistoryOperation.Create,
            PatientHistoryOperation.Delete => FhirHistoryOperation.Delete,
            _ => FhirHistoryOperation.Update
        };
    }
}
