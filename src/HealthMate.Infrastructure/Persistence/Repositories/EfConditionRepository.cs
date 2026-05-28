using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Persistence.Repositories;

public sealed class EfConditionRepository(HealthMateContext context) : IConditionRepository
{
    public Task<Condition?> GetByIdAsync(int conditionId, CancellationToken ct)
    {
        return context.Conditions.FirstOrDefaultAsync(condition => condition.Id == conditionId, ct);
    }

    public async Task AddAsync(Condition condition, CancellationToken ct)
    {
        await context.Conditions.AddAsync(condition, ct);
    }

    public Task<bool> DiseaseExistsAsync(int diseaseId, CancellationToken ct)
    {
        return context.Diseases.AnyAsync(disease => disease.Disease_Id == diseaseId, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return context.SaveChangesAsync(ct);
    }
}
