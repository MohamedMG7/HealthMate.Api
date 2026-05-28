using FluentValidation;
using HealthMate.Domain.Common;

namespace HealthMate.Application.Conditions.Commands;

public sealed class RecordConditionCommandValidator : AbstractValidator<RecordConditionCommand>
{
    public RecordConditionCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(command => command.EncounterId).GreaterThan(0);
        RuleFor(command => command.DiseaseId).GreaterThan(0);
        RuleFor(command => command.Severity).IsInEnum();
        RuleFor(command => command.ClinicalStatus).IsInEnum();
        RuleFor(command => command.DateRecorded)
            .Must(date => date <= clock.UtcNow.UtcDateTime.AddMinutes(1))
            .WithMessage("DateRecorded cannot be in the future.");
    }
}
