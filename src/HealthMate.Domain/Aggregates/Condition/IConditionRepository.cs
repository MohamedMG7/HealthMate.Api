namespace HealthMate.Domain.Aggregates.Condition;

public interface IConditionRepository
{
    Task<Condition?> GetByIdAsync(int conditionId, CancellationToken ct);
    Task AddAsync(Condition condition, CancellationToken ct);
    Task<bool> DiseaseExistsAsync(int diseaseId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
