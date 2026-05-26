using FluentAssertions;
using HealthMate.Application.Patients.Commands;
using HealthMate.Application.Patients.Validators;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Application;

public sealed class RegisterHumanPatientCommandValidatorTests
{
    [Fact]
    public async Task Rejects_bad_patient_shape()
    {
        var validator = new RegisterHumanPatientCommandValidator();
        var command = new RegisterHumanPatientCommand(
            "123",
            "patient_zero_national_id.png",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DomainGender.Female,
            "",
            "",
            "",
            -1,
            -1);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(RegisterHumanPatientCommand.NationalId),
            nameof(RegisterHumanPatientCommand.BirthDate),
            nameof(RegisterHumanPatientCommand.Governorate),
            nameof(RegisterHumanPatientCommand.City),
            nameof(RegisterHumanPatientCommand.ApplicationUserId),
            nameof(RegisterHumanPatientCommand.Weight),
            nameof(RegisterHumanPatientCommand.Height)
        ]);
    }
}
