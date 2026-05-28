namespace HealthMate.Domain.Aggregates.Encounter;

public interface IEncounterRepository
{
    Task<Encounter?> GetByIdAsync(int encounterId, CancellationToken ct);
    Task AddAsync(Encounter encounter, CancellationToken ct);
    Task<bool> PatientExistsAsync(int patientId, CancellationToken ct);
    Task<bool> HealthCareProviderExistsAsync(int healthCareProviderId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
