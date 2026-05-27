using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthMate.Infrastructure.Fhir;

public sealed class PatientHistoryWriter(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Capture(DbContext? context)
    {
        if (context is not HealthMateContext healthMateContext)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var entries = healthMateContext.ChangeTracker.Entries<Patient>()
            .Where(static e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        // Idempotent: any PatientHistory rows already queued in this save cycle mean a duplicate
        // interceptor registration already captured the change. Skip those patients.
        var alreadyCaptured = healthMateContext.ChangeTracker
            .Entries<PatientHistory>()
            .Where(static h => h.State == EntityState.Added)
            .Select(static h => h.Entity.Patient_Fhir_Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            var patient = entry.Entity;
            if (!string.IsNullOrWhiteSpace(patient.FhirId) && alreadyCaptured.Contains(patient.FhirId))
            {
                continue;
            }

            var user = patient.ApplicationUserId is null
                ? null
                : healthMateContext.Users.Local.FirstOrDefault(u => u.Id == patient.ApplicationUserId)
                    ?? healthMateContext.Users.Find(patient.ApplicationUserId);

            healthMateContext.PatientHistories.Add(new PatientHistory
            {
                Patient_Id = patient.Id,
                Patient_Fhir_Id = patient.FhirId,
                NationalId = patient.NationalId.Value,
                NationalIdImageUrl = patient.NationalIdImageUrl,
                BirthDate = patient.BirthDate,
                Gender = patient.Gender,
                Governorate = patient.Governorate.Value,
                City = patient.City.Value,
                IsVerified = patient.IsVerified,
                ApplicationUserId = patient.ApplicationUserId,
                Name = user is null ? null : $"{user.First_Name} {user.Last_Name}".Trim(),
                PhoneE164 = user?.PhoneNumber,
                Email = user?.Email,
                Weight = patient.Weight,
                Height = patient.Height,
                LastUpdated = patient.LastUpdated,
                RowVersion = patient.RowVersion,
                IsDeleted = patient.IsDeleted,
                DeletedAt = patient.DeletedAt,
                OperationType = ResolveOperation(entry),
                RecordedAt = now
            });
        }
    }

    private static PatientHistoryOperation ResolveOperation(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Patient> entry)
    {
        if (entry.State == EntityState.Added)
        {
            return PatientHistoryOperation.Create;
        }

        if (entry.State == EntityState.Deleted)
        {
            return PatientHistoryOperation.Delete;
        }

        var isDeleted = entry.Property(p => p.IsDeleted);
        if (isDeleted.IsModified && isDeleted.OriginalValue == false && isDeleted.CurrentValue)
        {
            return PatientHistoryOperation.Delete;
        }

        return PatientHistoryOperation.Update;
    }
}
