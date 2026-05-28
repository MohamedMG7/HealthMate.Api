using FluentAssertions;
using HealthMate.Application.Observations.Commands;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Tests.Infrastructure;

namespace HealthMate.Tests.Application.Observations;

public sealed class RecordObservationCommandValidatorTests
{
    [Fact]
    public async Task Rejects_bad_record_observation_shape()
    {
        var validator = new RecordObservationCommandValidator(FixedDateTimeProvider.Instance);
        var command = new RecordObservationCommand(
            0,
            (ObservationCategory)999,
            "   ",
            "   ",
            0,
            "   ",
            null,
            null,
            FixedDateTimeProvider.Instance.UtcNow.UtcDateTime.AddMinutes(2),
            null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(RecordObservationCommand.EncounterId),
            nameof(RecordObservationCommand.Category),
            nameof(RecordObservationCommand.Code),
            nameof(RecordObservationCommand.CodeDisplayName),
            nameof(RecordObservationCommand.ValueQuantity),
            nameof(RecordObservationCommand.ValueUnit),
            nameof(RecordObservationCommand.DateOfObservation)
        ]);
    }

    [Fact]
    public async Task Accepts_valid_record_observation_shape()
    {
        var validator = new RecordObservationCommandValidator(FixedDateTimeProvider.Instance);
        var command = new RecordObservationCommand(
            1,
            ObservationCategory.VitalSigns,
            "hr",
            "Heart rate",
            72,
            "bpm",
            "normal",
            null,
            FixedDateTimeProvider.Instance.UtcNow.UtcDateTime,
            null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
