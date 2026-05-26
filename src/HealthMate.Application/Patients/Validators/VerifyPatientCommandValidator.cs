using FluentValidation;
using HealthMate.Application.Patients.Commands;

namespace HealthMate.Application.Patients.Validators;

public sealed class VerifyPatientCommandValidator : AbstractValidator<VerifyPatientCommand>
{
    public VerifyPatientCommandValidator()
    {
        RuleFor(command => command.PatientId).GreaterThan(0);
        RuleFor(command => command.Reason)
            .NotEmpty()
            .When(static command => !command.Approve)
            .WithMessage("Rejection reason is required when rejecting a patient.");
    }
}
