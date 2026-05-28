using FluentValidation;
using HealthMate.Domain.Common;

namespace HealthMate.Application.Observations.Commands;

public sealed class RecordObservationCommandValidator : AbstractValidator<RecordObservationCommand>
{
    public RecordObservationCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(command => command.EncounterId).GreaterThan(0);
        RuleFor(command => command.Category).IsInEnum();
        RuleFor(command => command.DateOfObservation)
            .Must(date => date <= clock.UtcNow.UtcDateTime.AddMinutes(1))
            .WithMessage("DateOfObservation cannot be in the future.");
        RuleFor(command => command.ValueQuantity)
            .GreaterThan(0)
            .When(command => command.ValueQuantity.HasValue);
        RuleFor(command => command.ValueUnit)
            .Must(BeNullOrNonWhitespace)
            .WithMessage("ValueUnit must not be empty.")
            .When(command => command.ValueQuantity.HasValue || command.ValueUnit is not null);
        RuleFor(command => command.Code)
            .Must(BeNullOrNonWhitespace)
            .WithMessage("Code must not be empty.")
            .When(command => command.Code is not null);
        RuleFor(command => command.CodeDisplayName)
            .Must(BeNullOrNonWhitespace)
            .WithMessage("CodeDisplayName must not be empty.")
            .When(command => command.CodeDisplayName is not null);
    }

    private static bool BeNullOrNonWhitespace(string? value)
    {
        return value is null || !string.IsNullOrWhiteSpace(value);
    }
}
