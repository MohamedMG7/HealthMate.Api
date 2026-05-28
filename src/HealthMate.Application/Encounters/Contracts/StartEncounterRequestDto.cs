namespace HealthMate.Application.Encounters.Contracts;

public sealed record StartEncounterRequestDto(
    int PatientId,
    int HealthCareProviderId,
    string ReasonToVisit);
