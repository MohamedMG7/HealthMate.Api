using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Condition;

public sealed class ConditionNotFoundException : DomainException
{
    public ConditionNotFoundException(int conditionId)
        : base($"Condition '{conditionId}' was not found.")
    {
        ConditionId = conditionId;
    }

    public int ConditionId { get; }
}
