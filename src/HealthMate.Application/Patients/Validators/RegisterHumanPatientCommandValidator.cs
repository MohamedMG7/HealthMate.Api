using FluentValidation;
using HealthMate.Application.Patients.Commands;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;

namespace HealthMate.Application.Patients.Validators;

public sealed class RegisterHumanPatientCommandValidator : AbstractValidator<RegisterHumanPatientCommand>
{
    public RegisterHumanPatientCommandValidator()
    {
        RuleFor(command => command.NationalId)
            .NotEmpty()
            .Matches("^\\d{14}$")
            .WithMessage("National id must be exactly 14 digits.");

        RuleFor(command => command.BirthDate)
            .NotEmpty()
            .Must(static birthDate => birthDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Birth date cannot be in the future.");

        RuleFor(command => command.Gender).IsInEnum();
        RuleFor(command => command.Governorate).NotEmpty().MaximumLength(Governorate.MaxLength);
        RuleFor(command => command.City).NotEmpty().MaximumLength(City.MaxLength);
        RuleFor(command => command.ApplicationUserId).NotEmpty();
        RuleFor(command => command.Weight).GreaterThan(0).When(command => command.Weight.HasValue);
        RuleFor(command => command.Height).GreaterThan(0).When(command => command.Height.HasValue);
    }
}
