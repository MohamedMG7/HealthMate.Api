using HealthMate.Application.Common;

namespace HealthMate.Application.Encounters.Commands;

public sealed record StartEncounterCommand(
    int PatientId,
    int HealthCareProviderId,
    string ReasonToVisit) : ICommand<StartEncounterResult>;

public sealed record StartEncounterResult(int EncounterId, string EncounterFhirId);
