using FluentValidation;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;

namespace HealthMate.Application.Encounters.Commands;

public sealed class StartEncounterCommandValidator : AbstractValidator<StartEncounterCommand>
{
    public StartEncounterCommandValidator()
    {
        RuleFor(command => command.PatientId).GreaterThan(0);
        RuleFor(command => command.HealthCareProviderId).GreaterThan(0);
        RuleFor(command => command.ReasonToVisit)
            .NotEmpty()
            .MinimumLength(ReasonToVisit.MinLength)
            .MaximumLength(ReasonToVisit.MaxLength);
    }
}
