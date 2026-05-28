using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Persistence.Repositories;

public sealed class EfEncounterRepository(HealthMateContext context) : IEncounterRepository
{
    public Task<Encounter?> GetByIdAsync(int encounterId, CancellationToken ct)
    {
        return context.Encounters.FirstOrDefaultAsync(encounter => encounter.Id == encounterId, ct);
    }

    public async Task AddAsync(Encounter encounter, CancellationToken ct)
    {
        await context.Encounters.AddAsync(encounter, ct);
    }

    public Task<bool> PatientExistsAsync(int patientId, CancellationToken ct)
    {
        return context.Patients.AnyAsync(patient => patient.Id == patientId, ct);
    }

    public Task<bool> HealthCareProviderExistsAsync(int healthCareProviderId, CancellationToken ct)
    {
        return context.HealthCareProviders.AnyAsync(
            provider => provider.HealthCareProvider_Id == healthCareProviderId,
            ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return context.SaveChangesAsync(ct);
    }
}
