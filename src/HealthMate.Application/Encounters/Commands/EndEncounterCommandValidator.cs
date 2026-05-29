using FluentValidation;

namespace HealthMate.Application.Encounters.Commands;

public sealed class EndEncounterCommandValidator : AbstractValidator<EndEncounterCommand>
{
    public EndEncounterCommandValidator()
    {
        RuleFor(command => command.EncounterId).GreaterThan(0);
        RuleFor(command => command.TreatmentPlan).NotEmpty();
    }
}
