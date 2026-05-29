using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Encounter;

namespace HealthMate.Application.Encounters.Commands;

public sealed record EndEncounterCommand(
    int EncounterId,
    string TreatmentPlan,
    string? Note) : ICommand<EndEncounterResult>;

public sealed record EndEncounterResult(
    int EncounterId,
    DateTime EndDate,
    EncounterStatus Status);
