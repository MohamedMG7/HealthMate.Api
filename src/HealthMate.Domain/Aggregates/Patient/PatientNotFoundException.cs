using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Patient;

public sealed class PatientNotFoundException : DomainException
{
    public PatientNotFoundException(int patientId)
        : base($"Patient '{patientId}' was not found.")
    {
        PatientId = patientId;
    }

    public int PatientId { get; }
}
