using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Condition;

namespace HealthMate.Application.Conditions.Commands;

public sealed record RecordConditionCommand(
    int EncounterId,
    int DiseaseId,
    Severity Severity,
    ClinicalStatus ClinicalStatus,
    DateTime DateRecorded,
    string? Note) : ICommand<RecordConditionResult>;

public sealed record RecordConditionResult(int ConditionId, string ConditionFhirId, int PatientId);
