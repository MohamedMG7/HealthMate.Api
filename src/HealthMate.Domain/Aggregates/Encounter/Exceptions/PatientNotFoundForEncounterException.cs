using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Encounter;

public sealed class PatientNotFoundForEncounterException : DomainException
{
    public PatientNotFoundForEncounterException(int patientId)
        : base($"Patient '{patientId}' was not found for encounter start.")
    {
        PatientId = patientId;
    }

    public int PatientId { get; }
}
