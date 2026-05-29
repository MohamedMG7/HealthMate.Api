using FluentValidation;

namespace HealthMate.Application.Prescriptions.Commands;

public sealed class WritePrescriptionCommandValidator : AbstractValidator<WritePrescriptionCommand>
{
    public WritePrescriptionCommandValidator()
    {
        RuleFor(command => command.EncounterId).GreaterThan(0);
        RuleFor(command => command.Medicines)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .Must(medicines => medicines.Count > 0);

        RuleForEach(command => command.Medicines).ChildRules(line =>
        {
            line.RuleFor(medicine => medicine.MedicineId).GreaterThan(0);
            line.RuleFor(medicine => medicine.Dosage).NotEmpty();
            line.RuleFor(medicine => medicine.FrequencyInHours).GreaterThan(0);
            line.RuleFor(medicine => medicine.DurationInDays).GreaterThan(0);
        });
    }
}
