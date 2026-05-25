using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthMate.Infrastructure.Fhir;

public sealed class PatientLastUpdatedInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is not HealthMateContext healthMateContext)
        {
            return;
        }

        var now = clock.GetUtcNow();
        foreach (var entry in healthMateContext.ChangeTracker.Entries<Patient>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Entity.Patient_Fhir_Id))
            {
                entry.Entity.Patient_Fhir_Id = Guid.NewGuid().ToString();
            }

            entry.Entity.LastUpdated = now;
            entry.Entity.RowVersion = entry.State == EntityState.Added
                ? Math.Max(entry.Entity.RowVersion, 1)
                : entry.Entity.RowVersion + 1;
        }
    }
}
