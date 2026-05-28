using FluentAssertions;
using HealthMate.Application.Encounters.Commands;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;

namespace HealthMate.Tests.Application.Encounters;

public sealed class StartEncounterCommandValidatorTests
{
    [Fact]
    public async Task Rejects_bad_start_encounter_shape()
    {
        var validator = new StartEncounterCommandValidator();
        var command = new StartEncounterCommand(
            0,
            -1,
            new string('a', ReasonToVisit.MaxLength + 1));

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(StartEncounterCommand.PatientId),
            nameof(StartEncounterCommand.HealthCareProviderId),
            nameof(StartEncounterCommand.ReasonToVisit)
        ]);
    }

    [Fact]
    public async Task Accepts_valid_start_encounter_shape()
    {
        var validator = new StartEncounterCommandValidator();
        var command = new StartEncounterCommand(1, 2, "Synthetic visit reason");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
