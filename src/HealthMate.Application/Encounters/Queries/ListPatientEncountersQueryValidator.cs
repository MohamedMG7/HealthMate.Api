using FluentValidation;

namespace HealthMate.Application.Encounters.Queries;

public sealed class ListPatientEncountersQueryValidator : AbstractValidator<ListPatientEncountersQuery>
{
    public ListPatientEncountersQueryValidator()
    {
        RuleFor(q => q.PatientId).GreaterThan(0);
    }
}
