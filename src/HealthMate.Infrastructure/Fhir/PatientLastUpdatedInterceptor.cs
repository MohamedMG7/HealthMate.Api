using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.DbHelper;
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

            if (string.IsNullOrWhiteSpace(entry.Entity.FhirId))
            {
                entry.Entity.AssignFhirId(Guid.NewGuid().ToString());
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.ApplyPersistenceVersion(now, Math.Max(entry.Entity.RowVersion, 1));
                continue;
            }

            // Idempotent: if RowVersion is already flagged Modified, we (or a duplicate registration of this interceptor)
            // have already bumped it for this save cycle. Skip to avoid stacking increments.
            var rowVersionProperty = entry.Property(p => p.RowVersion);
            if (rowVersionProperty.IsModified)
            {
                continue;
            }

            entry.Entity.ApplyPersistenceVersion(now, entry.Entity.RowVersion + 1);
            rowVersionProperty.IsModified = true;
        }
    }
}
