using HealthMate.Domain.Aggregates.Encounter;

namespace HealthMate.Application.Encounters.Contracts;

public sealed record EncounterHistoryItem(
    int EncounterId,
    DateTime StartDate,
    DateTime EndDate,
    EncounterStatus Status,
    string ReasonToVisit,
    int HealthCareProviderId);
