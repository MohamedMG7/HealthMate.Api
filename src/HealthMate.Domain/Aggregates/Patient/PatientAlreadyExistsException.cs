using HealthMate.Domain.Common;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;

namespace HealthMate.Domain.Aggregates.Patient;

public sealed class PatientAlreadyExistsException : DomainException
{
    public PatientAlreadyExistsException(NationalId nationalId)
        : base($"Patient with national id '{nationalId.Value}' already exists.")
    {
        NationalId = nationalId;
    }

    public NationalId NationalId { get; }
}
