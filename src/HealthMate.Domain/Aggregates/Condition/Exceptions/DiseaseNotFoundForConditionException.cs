using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Condition;

public sealed class DiseaseNotFoundForConditionException : DomainException
{
    public DiseaseNotFoundForConditionException(int diseaseId)
        : base($"Disease '{diseaseId}' was not found for condition recording.")
    {
        DiseaseId = diseaseId;
    }

    public int DiseaseId { get; }
}
