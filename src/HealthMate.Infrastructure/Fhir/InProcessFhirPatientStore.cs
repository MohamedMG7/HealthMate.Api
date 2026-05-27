using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Common;
using HealthMate.Fhir.Ports;
using HealthMate.Fhir.Ports.Dtos;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Infrastructure.Fhir;

public sealed class InProcessFhirPatientStore(
    HealthMateContext context,
    IDateTimeProvider clock) : IFhirPatientStore
{
    private const string FhirPlaceholderNationalIdImage = "fhir_patient_national_id.png";

    public async Task<FhirPatientSnapshot?> ReadAsync(string fhirId, CancellationToken ct)
    {
        var patient = await context.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.FhirId == fhirId, ct);

        if (patient is null)
        {
            return null;
        }

        var user = await FindUserAsync(patient.ApplicationUserId, ct);
        return ToSnapshot(patient, user);
    }

    public async Task<FhirPatientSearchResult> SearchAsync(FhirPatientSearchQuery query, CancellationToken ct)
    {
        var patients = context.Patients
            .AsNoTracking()
            .Where(static p => !p.IsDeleted);

        if (query.Ids.Count > 0)
        {
            patients = patients.Where(p => query.Ids.Contains(p.FhirId));
        }

        foreach (var filter in query.LastUpdated)
        {
            patients = ApplyDateTimeFilter(patients, filter);
        }

        if (query.Name is not null)
        {
            var userIds = await FindUserIdsByNameAsync(query.Name, ct);
            patients = patients.Where(p => p.ApplicationUserId != null && userIds.Contains(p.ApplicationUserId));
        }

        if (query.Identifier is not null)
        {
            var nationalId = NationalId.FromTrusted(query.Identifier.Value);
            patients = patients.Where(p => p.NationalId == nationalId);
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

        var users = await FindUsersByPatientPageAsync(page, ct);
        return new FhirPatientSearchResult(page.Select(patient => ToSnapshot(patient, users.GetValueOrDefault(patient.ApplicationUserId))).ToArray(), total, query.Offset, query.Count);
    }

    public async Task<FhirPatientSnapshot> CreateAsync(FhirPatientSnapshot snapshot, CancellationToken ct)
    {
        var patient = Patient.Create(
            NationalId.Create(snapshot.NationalId),
            snapshot.BirthDate,
            ToGender(snapshot.Gender),
            Governorate.Create(snapshot.Governorate),
            City.Create(snapshot.City),
            userId: null,
            nationalIdImageUrl: FhirPlaceholderNationalIdImage);

        if (!string.IsNullOrWhiteSpace(snapshot.FhirId))
        {
            patient.AssignFhirId(snapshot.FhirId);
        }

        if (snapshot.IsDeleted)
        {
            patient.MarkSoftDeleted(clock);
        }

        context.Patients.Add(patient);
        await context.SaveChangesAsync(ct);

        return (await ReadAsync(patient.FhirId, ct))!;
    }

    public async Task<FhirPatientSnapshot> UpdateAsync(FhirPatientSnapshot snapshot, uint expectedVersion, CancellationToken ct)
    {
        var patient = await context.Patients
            .SingleOrDefaultAsync(p => p.FhirId == snapshot.FhirId, ct);

        if (patient is null)
        {
            throw new FhirNotFoundException("Patient", snapshot.FhirId);
        }

        if (patient.RowVersion != expectedVersion)
        {
            throw new FhirConcurrencyException("Patient", snapshot.FhirId);
        }

        patient.UpdateDemographics(
            NationalId.Create(snapshot.NationalId),
            snapshot.BirthDate,
            ToGender(snapshot.Gender),
            Governorate.Create(snapshot.Governorate),
            City.Create(snapshot.City));
        // IsVerified is admin-managed and intentionally not touched here; FHIR PUT must not flip verification status.
        if (snapshot.IsDeleted)
        {
            patient.MarkSoftDeleted(clock);
        }
        else
        {
            patient.RestoreFromSoftDelete();
        }

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
        var patient = await context.Patients.SingleOrDefaultAsync(p => p.FhirId == fhirId, ct);
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

        patient.MarkSoftDeleted(clock);

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
            "name" when sort.Descending && thenBy => ordered!.ThenByDescending(p => p.ApplicationUserId ?? string.Empty),
            "name" when sort.Descending => query.OrderByDescending(p => p.ApplicationUserId ?? string.Empty),
            "name" when thenBy => ordered!.ThenBy(p => p.ApplicationUserId ?? string.Empty),
            "name" => query.OrderBy(p => p.ApplicationUserId ?? string.Empty),
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

    private static FhirPatientSnapshot ToSnapshot(Patient patient, ApplicationUser? user)
    {
        return new FhirPatientSnapshot(
            patient.FhirId,
            patient.NationalId.Value,
            user is null ? null : $"{user.First_Name} {user.Last_Name}".Trim(),
            patient.BirthDate,
            FromGender(patient.Gender),
            patient.Governorate.Value,
            patient.City.Value,
            user?.PhoneNumber,
            user?.Email,
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

    private static DomainGender ToGender(string gender)
    {
        return gender.Equals("female", StringComparison.OrdinalIgnoreCase)
            ? DomainGender.Female
            : DomainGender.Male;
    }

    private static bool TryParseGender(string gender, out DomainGender parsed)
    {
        if (gender.Equals("male", StringComparison.OrdinalIgnoreCase))
        {
            parsed = DomainGender.Male;
            return true;
        }

        if (gender.Equals("female", StringComparison.OrdinalIgnoreCase))
        {
            parsed = DomainGender.Female;
            return true;
        }

        parsed = default;
        return false;
    }

    private static string FromGender(DomainGender gender)
    {
        return gender == DomainGender.Female ? "female" : "male";
    }

    private async Task<ApplicationUser?> FindUserAsync(string? userId, CancellationToken ct)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, ct);
    }

    private async Task<string[]> FindUserIdsByNameAsync(FhirStringSearch name, CancellationToken ct)
    {
        var lowered = name.Value.ToLowerInvariant();
        var users = context.Users.AsNoTracking();

        users = name.Exact
            ? users.Where(user => (user.First_Name + " " + user.Last_Name).ToLower() == lowered)
            : users.Where(user => (user.First_Name + " " + user.Last_Name).ToLower().Contains(lowered));

        return await users.Select(user => user.Id).ToArrayAsync(ct);
    }

    private async Task<Dictionary<string, ApplicationUser>> FindUsersByPatientPageAsync(IReadOnlyCollection<Patient> patients, CancellationToken ct)
    {
        var userIds = patients
            .Select(patient => patient.ApplicationUserId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (userIds.Length == 0)
        {
            return new Dictionary<string, ApplicationUser>(StringComparer.Ordinal);
        }

        var users = await context.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync(ct);

        return users.ToDictionary(user => user.Id, StringComparer.Ordinal);
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
