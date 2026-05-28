using FluentAssertions;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Domain.Common;
using HealthMate.Tests.Infrastructure;

namespace HealthMate.Tests.Domain;

public sealed class ObservationTests
{
    [Fact]
    public void Record_creates_observation_with_provided_clock_and_category()
    {
        var observedAt = FixedDateTimeProvider.Instance.UtcNow.UtcDateTime;

        var observation = Observation.Record(
            1,
            2,
            ObservationCategory.VitalSigns,
            "hr",
            "Heart rate",
            72,
            "bpm",
            "normal",
            3,
            observedAt,
            "provided-identifier",
            FixedDateTimeProvider.Instance);

        observation.PatientId.Should().Be(1);
        observation.EncounterId.Should().Be(2);
        observation.Category.Should().Be(ObservationCategory.VitalSigns);
        observation.Code.Should().Be("hr");
        observation.CodeDisplayName.Should().Be("Heart rate");
        observation.ValueQuantity.Should().Be(72);
        observation.ValueUnit.Should().Be("bpm");
        observation.Interpretation.Should().Be("normal");
        observation.BodySiteId.Should().Be(3);
        observation.DateOfObservation.Should().Be(observedAt);
        observation.NameIdentifier.Should().Be("provided-identifier");
        observation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Record_generates_name_identifier_when_caller_omits()
    {
        var observation = ValidObservation(nameIdentifier: "   ");

        observation.NameIdentifier.Should().MatchRegex("^OBS-code-[0-9a-f]{8}$");
    }

    [Fact]
    public void Record_preserves_caller_supplied_name_identifier()
    {
        var observation = ValidObservation(nameIdentifier: "caller-supplied");

        observation.NameIdentifier.Should().Be("caller-supplied");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_rejects_invalid_patient_id(int patientId)
    {
        Action act = () => ValidObservation(patientId: patientId);

        act.Should().Throw<DomainException>()
            .WithMessage("Patient id must be greater than zero.");
    }

    [Fact]
    public void Record_rejects_invalid_category()
    {
        Action act = () => ValidObservation(category: (ObservationCategory)999);

        act.Should().Throw<DomainException>()
            .WithMessage("Observation category must be a defined value.");
    }

    [Fact]
    public void Record_rejects_future_date_of_observation_beyond_slack()
    {
        var future = FixedDateTimeProvider.Instance.UtcNow.UtcDateTime.AddMinutes(2);

        Action act = () => ValidObservation(dateOfObservation: future);

        act.Should().Throw<DomainException>()
            .WithMessage("Date of observation cannot be in the future.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_rejects_non_positive_value_quantity_when_present(decimal valueQuantity)
    {
        Action act = () => ValidObservation(valueQuantity: valueQuantity);

        act.Should().Throw<DomainException>()
            .WithMessage("Value quantity must be greater than zero.");
    }

    [Fact]
    public void Record_rejects_blank_value_unit_when_value_quantity_present()
    {
        Action act = () => ValidObservation(valueQuantity: 1, valueUnit: "   ");

        act.Should().Throw<DomainException>()
            .WithMessage("Value unit must not be empty.");
    }

    private static Observation ValidObservation(
        int patientId = 1,
        ObservationCategory category = ObservationCategory.Clinical,
        decimal? valueQuantity = 1,
        string? valueUnit = "unit",
        DateTime? dateOfObservation = null,
        string? nameIdentifier = "identifier")
    {
        return Observation.Record(
            patientId,
            2,
            category,
            "code",
            "Code display",
            valueQuantity,
            valueUnit,
            "normal",
            null,
            dateOfObservation ?? FixedDateTimeProvider.Instance.UtcNow.UtcDateTime,
            nameIdentifier,
            FixedDateTimeProvider.Instance);
    }
}
