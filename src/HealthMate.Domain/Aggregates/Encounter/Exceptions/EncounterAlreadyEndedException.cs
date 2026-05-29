using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Encounter;

public sealed class EncounterAlreadyEndedException : DomainException
{
    public EncounterAlreadyEndedException(int encounterId, EncounterStatus currentStatus)
        : base($"Encounter {encounterId} is already {currentStatus.ToString().ToLowerInvariant()} and cannot be ended again.")
    {
        EncounterId = encounterId;
        CurrentStatus = currentStatus;
    }

    public int EncounterId { get; }
    public EncounterStatus CurrentStatus { get; }
}
