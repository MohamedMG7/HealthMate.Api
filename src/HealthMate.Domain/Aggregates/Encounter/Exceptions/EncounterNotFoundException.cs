using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Encounter;

public sealed class EncounterNotFoundException : DomainException
{
    public EncounterNotFoundException(int encounterId)
        : base($"Encounter '{encounterId}' was not found.")
    {
        EncounterId = encounterId;
    }

    public int EncounterId { get; }
}
