using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Encounters.Commands;

public sealed class StartEncounterCommandHandler(
    IEncounterRepository encounterRepository,
    IDateTimeProvider clock,
    ILogger<StartEncounterCommandHandler> logger)
    : IHandler<StartEncounterCommand, StartEncounterResult>
{
    public async Task<StartEncounterResult> HandleAsync(StartEncounterCommand request, CancellationToken ct)
    {
        var reason = ReasonToVisit.Create(request.ReasonToVisit);

        if (!await encounterRepository.PatientExistsAsync(request.PatientId, ct))
        {
            throw new PatientNotFoundForEncounterException(request.PatientId);
        }

        if (!await encounterRepository.HealthCareProviderExistsAsync(request.HealthCareProviderId, ct))
        {
            throw new HealthCareProviderNotFoundForEncounterException(request.HealthCareProviderId);
        }

        var encounter = Encounter.Start(request.PatientId, request.HealthCareProviderId, reason, clock);
        await encounterRepository.AddAsync(encounter, ct);
        await encounterRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Started encounter {EncounterId} for patient {PatientId} by hcp {HcpId}",
            encounter.Id,
            encounter.PatientId,
            encounter.HealthCareProviderId);

        return new StartEncounterResult(encounter.Id, encounter.FhirId);
    }
}
