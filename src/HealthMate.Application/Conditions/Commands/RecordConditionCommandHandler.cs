using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Conditions.Commands;

public sealed class RecordConditionCommandHandler(
    IEncounterRepository encounterRepository,
    IConditionRepository conditionRepository,
    IDateTimeProvider clock,
    ILogger<RecordConditionCommandHandler> logger)
    : IHandler<RecordConditionCommand, RecordConditionResult>
{
    public async Task<RecordConditionResult> HandleAsync(RecordConditionCommand request, CancellationToken ct)
    {
        var encounter = await encounterRepository.GetByIdAsync(request.EncounterId, ct);
        if (encounter is null)
        {
            throw new EncounterNotFoundException(request.EncounterId);
        }

        if (encounter.Status != EncounterStatus.Active)
        {
            logger.LogWarning(
                "Late entry: recording condition on {Status} encounter {EncounterId} for patient {PatientId}",
                encounter.Status,
                encounter.Id,
                encounter.PatientId);
        }

        if (!await conditionRepository.DiseaseExistsAsync(request.DiseaseId, ct))
        {
            throw new DiseaseNotFoundForConditionException(request.DiseaseId);
        }

        var condition = Condition.Record(
            encounter.PatientId,
            encounter.Id,
            request.DiseaseId,
            request.Severity,
            request.ClinicalStatus,
            request.DateRecorded,
            request.Note,
            clock);

        await conditionRepository.AddAsync(condition, ct);
        await conditionRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded condition {ConditionId} on encounter {EncounterId} for patient {PatientId} with disease {DiseaseId}",
            condition.Id,
            encounter.Id,
            encounter.PatientId,
            request.DiseaseId);

        return new RecordConditionResult(condition.Id, condition.FhirId, encounter.PatientId);
    }
}
