using FluentAssertions;
using HealthMate.Application.Conditions.Commands;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Tests.Infrastructure;

namespace HealthMate.Tests.Application.Conditions;

public sealed class RecordConditionCommandValidatorTests
{
    [Fact]
    public async Task Rejects_bad_record_condition_shape()
    {
        var validator = new RecordConditionCommandValidator(FixedDateTimeProvider.Instance);
        var command = new RecordConditionCommand(
            0,
            0,
            (Severity)999,
            (ClinicalStatus)999,
            FixedDateTimeProvider.Instance.UtcNow.UtcDateTime.AddMinutes(2),
            null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(RecordConditionCommand.EncounterId),
            nameof(RecordConditionCommand.DiseaseId),
            nameof(RecordConditionCommand.Severity),
            nameof(RecordConditionCommand.ClinicalStatus),
            nameof(RecordConditionCommand.DateRecorded)
        ]);
    }

    [Fact]
    public async Task Accepts_valid_record_condition_shape()
    {
        var validator = new RecordConditionCommandValidator(FixedDateTimeProvider.Instance);
        var command = new RecordConditionCommand(
            1,
            2,
            Severity.Mild,
            ClinicalStatus.Active,
            FixedDateTimeProvider.Instance.UtcNow.UtcDateTime,
            "test-note");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
