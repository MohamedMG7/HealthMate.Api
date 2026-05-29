using FluentAssertions;
using HealthMate.Application.Prescriptions.Commands;

namespace HealthMate.Tests.Application.Prescriptions;

public sealed class WritePrescriptionCommandValidatorTests
{
    [Fact]
    public async Task Rejects_bad_write_prescription_shape()
    {
        var validator = new WritePrescriptionCommandValidator();
        var command = new WritePrescriptionCommand(
            0,
            null,
            [new WritePrescriptionMedicineLine(0, "", 0, 0)]);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain([
            nameof(WritePrescriptionCommand.EncounterId),
            "Medicines[0].MedicineId",
            "Medicines[0].Dosage",
            "Medicines[0].FrequencyInHours",
            "Medicines[0].DurationInDays"
        ]);
    }

    [Fact]
    public async Task Rejects_empty_medicine_list()
    {
        var validator = new WritePrescriptionCommandValidator();
        var command = new WritePrescriptionCommand(1, null, []);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain(nameof(WritePrescriptionCommand.Medicines));
    }

    [Fact]
    public async Task Accepts_valid_write_prescription_shape()
    {
        var validator = new WritePrescriptionCommandValidator();
        var command = new WritePrescriptionCommand(
            1,
            "Provider_Zero",
            [new WritePrescriptionMedicineLine(2, "10mg", 8, 5)]);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
