using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Condition;

public sealed class Condition : AggregateRoot<int>
{
    private Condition()
    {
    }

    public string FhirId { get; private set; } = null!;
    public int PatientId { get; private set; }
    public int? EncounterId { get; private set; }
    public int DiseaseId { get; private set; }
    public Severity Severity { get; private set; }
    public ClinicalStatus ClinicalStatus { get; private set; }
    public DateTime DateRecorded { get; private set; }
    public string? Note { get; private set; }

    private ConditionRecorder Recorder { get; set; } = ConditionRecorder.HealthCareProvider;
    private ConditionAddedByUserType AddedBy { get; set; } = ConditionAddedByUserType.HealthCareProvider;
    private bool IsOngoing { get; set; } = true;
    private bool IsChronic { get; set; }
    private int? BodySiteId { get; set; }

    public static Condition Record(
        int patientId,
        int? encounterId,
        int diseaseId,
        Severity severity,
        ClinicalStatus clinicalStatus,
        DateTime dateRecorded,
        string? note,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (patientId <= 0)
        {
            throw new DomainException("Patient id must be greater than zero.");
        }

        if (diseaseId <= 0)
        {
            throw new DomainException("Disease id must be greater than zero.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new DomainException("Severity must be a defined value.");
        }

        if (!Enum.IsDefined(clinicalStatus))
        {
            throw new DomainException("Clinical status must be a defined value.");
        }

        if (dateRecorded > clock.UtcNow.UtcDateTime.AddMinutes(1))
        {
            throw new DomainException("Date recorded cannot be in the future.");
        }

        return new Condition
        {
            PatientId = patientId,
            EncounterId = encounterId,
            DiseaseId = diseaseId,
            Severity = severity,
            ClinicalStatus = clinicalStatus,
            DateRecorded = dateRecorded,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Recorder = ConditionRecorder.HealthCareProvider,
            AddedBy = ConditionAddedByUserType.HealthCareProvider,
            IsOngoing = true,
            IsChronic = false,
            BodySiteId = null
        };
    }
}
