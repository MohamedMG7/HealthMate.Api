using HealthMate.Domain.Aggregates.Condition;

namespace HealthMate.Application.Conditions.Contracts;

public sealed record RecordConditionRequestDto(
    int DiseaseId,
    Severity Severity,
    ClinicalStatus ClinicalStatus,
    DateTime DateRecorded,
    string? Note);
