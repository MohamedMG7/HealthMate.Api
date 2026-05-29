namespace HealthMate.Application.Encounters.Contracts;

public sealed record EndEncounterRequestDto(string TreatmentPlan, string? Note);
