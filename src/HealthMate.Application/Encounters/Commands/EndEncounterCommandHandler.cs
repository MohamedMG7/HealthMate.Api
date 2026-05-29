using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Encounters.Commands;

public sealed class EndEncounterCommandHandler(
    IEncounterRepository encounterRepository,
    IDateTimeProvider clock,
    ILogger<EndEncounterCommandHandler> logger)
    : IHandler<EndEncounterCommand, EndEncounterResult>
{
    public async Task<EndEncounterResult> HandleAsync(EndEncounterCommand request, CancellationToken ct)
    {
        var encounter = await encounterRepository.GetByIdAsync(request.EncounterId, ct);
        if (encounter is null)
        {
            throw new EncounterNotFoundException(request.EncounterId);
        }

        encounter.End(request.TreatmentPlan, request.Note, clock);
        await encounterRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ended encounter {EncounterId} for patient {PatientId} by hcp {HcpId}",
            encounter.Id,
            encounter.PatientId,
            encounter.HealthCareProviderId);

        return new EndEncounterResult(encounter.Id, encounter.EndDate, encounter.Status);
    }
}
