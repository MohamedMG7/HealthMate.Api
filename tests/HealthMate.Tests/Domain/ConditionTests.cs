using System.Reflection;
using FluentAssertions;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Common;
using HealthMate.Tests.Infrastructure;

namespace HealthMate.Tests.Domain;

public sealed class ConditionTests
{
    [Fact]
    public void Record_creates_condition_with_active_status_and_provided_clock()
    {
        var recordedAt = FixedDateTimeProvider.Instance.UtcNow.UtcDateTime;

        var condition = Condition.Record(
            1,
            2,
            3,
            Severity.Moderate,
            ClinicalStatus.Active,
            recordedAt,
            "test-note",
            FixedDateTimeProvider.Instance);

        condition.PatientId.Should().Be(1);
        condition.EncounterId.Should().Be(2);
        condition.DiseaseId.Should().Be(3);
        condition.Severity.Should().Be(Severity.Moderate);
        condition.ClinicalStatus.Should().Be(ClinicalStatus.Active);
        condition.DateRecorded.Should().Be(recordedAt);
        condition.Note.Should().Be("test-note");
    }

    [Fact]
    public void Record_defaults_legacy_fields_to_HCP_recorder_and_ongoing()
    {
        var condition = ValidCondition();

        GetPrivate<ConditionRecorder>(condition, "Recorder").Should().Be(ConditionRecorder.HealthCareProvider);
        GetPrivate<ConditionAddedByUserType>(condition, "AddedBy").Should().Be(ConditionAddedByUserType.HealthCareProvider);
        GetPrivate<bool>(condition, "IsOngoing").Should().BeTrue();
        GetPrivate<bool>(condition, "IsChronic").Should().BeFalse();
        GetPrivate<int?>(condition, "BodySiteId").Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_rejects_invalid_patient_id(int patientId)
    {
        Action act = () => ValidCondition(patientId: patientId);

        act.Should().Throw<DomainException>()
            .WithMessage("Patient id must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_rejects_invalid_disease_id(int diseaseId)
    {
        Action act = () => ValidCondition(diseaseId: diseaseId);

        act.Should().Throw<DomainException>()
            .WithMessage("Disease id must be greater than zero.");
    }

    [Fact]
    public void Record_rejects_invalid_severity_enum()
    {
        Action act = () => ValidCondition(severity: (Severity)999);

        act.Should().Throw<DomainException>()
            .WithMessage("Severity must be a defined value.");
    }

    [Fact]
    public void Record_rejects_invalid_clinical_status_enum()
    {
        Action act = () => ValidCondition(clinicalStatus: (ClinicalStatus)999);

        act.Should().Throw<DomainException>()
            .WithMessage("Clinical status must be a defined value.");
    }

    [Fact]
    public void Record_rejects_future_date_recorded_beyond_slack()
    {
        var future = FixedDateTimeProvider.Instance.UtcNow.UtcDateTime.AddMinutes(2);

        Action act = () => ValidCondition(dateRecorded: future);

        act.Should().Throw<DomainException>()
            .WithMessage("Date recorded cannot be in the future.");
    }

    [Fact]
    public void Record_trims_note_and_treats_whitespace_as_null()
    {
        var trimmed = ValidCondition(note: "  test-note  ");
        var omitted = ValidCondition(note: "   ");

        trimmed.Note.Should().Be("test-note");
        omitted.Note.Should().BeNull();
    }

    private static Condition ValidCondition(
        int patientId = 1,
        int diseaseId = 2,
        Severity severity = Severity.Mild,
        ClinicalStatus clinicalStatus = ClinicalStatus.Active,
        DateTime? dateRecorded = null,
        string? note = "test-note")
    {
        return Condition.Record(
            patientId,
            3,
            diseaseId,
            severity,
            clinicalStatus,
            dateRecorded ?? FixedDateTimeProvider.Instance.UtcNow.UtcDateTime,
            note,
            FixedDateTimeProvider.Instance);
    }

    private static T GetPrivate<T>(Condition condition, string propertyName)
    {
        return (T)typeof(Condition)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(condition)!;
    }
}
