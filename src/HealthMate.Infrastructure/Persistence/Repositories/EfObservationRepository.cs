using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Persistence.Repositories;

public sealed class EfObservationRepository(HealthMateContext context) : IObservationRepository
{
    public Task<Observation?> GetByIdAsync(int observationId, CancellationToken ct)
    {
        return context.Observations.FirstOrDefaultAsync(observation => observation.Id == observationId, ct);
    }

    public async Task AddAsync(Observation observation, CancellationToken ct)
    {
        await context.Observations.AddAsync(observation, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return context.SaveChangesAsync(ct);
    }
}
