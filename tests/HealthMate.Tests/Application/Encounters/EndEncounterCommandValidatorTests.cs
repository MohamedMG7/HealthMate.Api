using FluentAssertions;
using HealthMate.Application.Encounters.Commands;

namespace HealthMate.Tests.Application.Encounters;

public sealed class EndEncounterCommandValidatorTests
{
    [Fact]
    public async Task Rejects_bad_end_encounter_shape()
    {
        var validator = new EndEncounterCommandValidator();
        var command = new EndEncounterCommand(0, "   ", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(EndEncounterCommand.EncounterId),
            nameof(EndEncounterCommand.TreatmentPlan)
        ]);
    }

    [Fact]
    public async Task Accepts_valid_end_encounter_shape()
    {
        var validator = new EndEncounterCommandValidator();
        var command = new EndEncounterCommand(1, "Synthetic treatment plan", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
