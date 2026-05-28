using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Encounter;

public sealed class Encounter : AggregateRoot<int>
{
    private Encounter()
    {
    }

    public string FhirId { get; private set; } = null!;
    public int PatientId { get; private set; }
    public int HealthCareProviderId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string? Location { get; private set; }
    public ReasonToVisit ReasonToVisit { get; private set; } = null!;
    public string TreatmentPlan { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public bool IsDeleted { get; private set; }
    public EncounterStatus Status { get; private set; }

    public static Encounter Start(
        int patientId,
        int healthCareProviderId,
        ReasonToVisit reason,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureIdsAreValid(patientId, healthCareProviderId);

        var startedAt = clock.UtcNow.UtcDateTime;
        return new Encounter
        {
            PatientId = patientId,
            HealthCareProviderId = healthCareProviderId,
            StartDate = startedAt,
            EndDate = startedAt,
            ReasonToVisit = reason,
            TreatmentPlan = string.Empty,
            IsDeleted = false,
            Status = EncounterStatus.Active
        };
    }

    public static Encounter CreateLegacy(
        int patientId,
        int healthCareProviderId,
        DateTime startDate,
        DateTime endDate,
        string? location,
        string reasonToVisit,
        string? treatmentPlan,
        string? note)
    {
        EnsureIdsAreValid(patientId, healthCareProviderId);

        return new Encounter
        {
            PatientId = patientId,
            HealthCareProviderId = healthCareProviderId,
            StartDate = startDate,
            EndDate = endDate,
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            ReasonToVisit = ReasonToVisit.Create(reasonToVisit),
            TreatmentPlan = string.IsNullOrWhiteSpace(treatmentPlan) ? string.Empty : treatmentPlan.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            IsDeleted = false,
            Status = EncounterStatus.Active
        };
    }

    private static void EnsureIdsAreValid(int patientId, int healthCareProviderId)
    {
        if (patientId <= 0)
        {
            throw new DomainException("Patient id must be greater than zero.");
        }

        if (healthCareProviderId <= 0)
        {
            throw new DomainException("Health care provider id must be greater than zero.");
        }
    }
}
