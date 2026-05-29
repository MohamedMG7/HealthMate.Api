using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Prescription;

public sealed class PrescriptionNotFoundException : DomainException
{
    public PrescriptionNotFoundException(int prescriptionId)
        : base($"Prescription '{prescriptionId}' was not found.")
    {
        PrescriptionId = prescriptionId;
    }

    public int PrescriptionId { get; }
}
