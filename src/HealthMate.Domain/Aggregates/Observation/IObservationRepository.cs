namespace HealthMate.Domain.Aggregates.Observation;

public interface IObservationRepository
{
    Task<Observation?> GetByIdAsync(int observationId, CancellationToken ct);
    Task AddAsync(Observation observation, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
