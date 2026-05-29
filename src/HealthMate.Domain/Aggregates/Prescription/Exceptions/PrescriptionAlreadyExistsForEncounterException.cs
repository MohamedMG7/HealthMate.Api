using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Prescription;

public sealed class PrescriptionAlreadyExistsForEncounterException : DomainException
{
    public PrescriptionAlreadyExistsForEncounterException(int encounterId)
        : base($"Prescription already exists for encounter '{encounterId}'.")
    {
        EncounterId = encounterId;
    }

    public int EncounterId { get; }
}
