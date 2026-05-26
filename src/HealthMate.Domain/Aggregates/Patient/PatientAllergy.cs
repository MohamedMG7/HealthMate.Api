using HealthMate.Domain.Aggregates.Patient.Enums;
using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Patient;

public sealed class PatientAllergy : Entity<int>
{
    private PatientAllergy()
    {
    }

    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public string Substance { get; private set; } = null!;
    public AllergySeverity Severity { get; private set; }
    public string? Reaction { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    internal static PatientAllergy Create(
        Patient patient,
        string substance,
        AllergySeverity severity,
        string? reaction,
        string? notes,
        IDateTimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(substance))
        {
            throw new DomainException("Allergy substance is required.");
        }

        return new PatientAllergy
        {
            Patient = patient,
            PatientId = patient.Patient_Id,
            Substance = substance.Trim(),
            Severity = severity,
            Reaction = string.IsNullOrWhiteSpace(reaction) ? null : reaction.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedAt = clock.UtcNow.UtcDateTime,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
}
