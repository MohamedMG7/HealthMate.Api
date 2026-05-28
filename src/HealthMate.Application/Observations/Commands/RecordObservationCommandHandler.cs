using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Observations.Commands;

public sealed class RecordObservationCommandHandler(
    IEncounterRepository encounterRepository,
    IObservationRepository observationRepository,
    IDateTimeProvider clock,
    ILogger<RecordObservationCommandHandler> logger)
    : IHandler<RecordObservationCommand, RecordObservationResult>
{
    public async Task<RecordObservationResult> HandleAsync(RecordObservationCommand request, CancellationToken ct)
    {
        var encounter = await encounterRepository.GetByIdAsync(request.EncounterId, ct);
        if (encounter is null)
        {
            throw new EncounterNotFoundException(request.EncounterId);
        }

        var observation = Observation.Record(
            encounter.PatientId,
            encounter.Id,
            request.Category,
            request.Code,
            request.CodeDisplayName,
            request.ValueQuantity,
            request.ValueUnit,
            request.Interpretation,
            request.BodySiteId,
            request.DateOfObservation,
            request.NameIdentifier,
            clock);

        await observationRepository.AddAsync(observation, ct);
        await observationRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded observation {ObservationId} on encounter {EncounterId} for patient {PatientId}",
            observation.Id,
            encounter.Id,
            encounter.PatientId);

        return new RecordObservationResult(observation.Id, observation.FhirId, encounter.PatientId);
    }
}
