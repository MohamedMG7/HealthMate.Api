using FluentAssertions;
using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Domain.Common;
using HealthMate.Tests.Infrastructure;

namespace HealthMate.Tests.Domain;

public sealed class PrescriptionTests
{
    [Fact]
    public void Write_creates_prescription_with_provided_clock()
    {
        var prescription = ValidPrescription();

        prescription.PatientId.Should().Be(1);
        prescription.EncounterId.Should().Be(2);
        prescription.Publisher.Should().Be("Provider_Zero");
        prescription.PrescriptionDate.Should().Be(FixedDateTimeProvider.Instance.UtcNow.UtcDateTime);
        prescription.Medicines.Should().HaveCount(1);
    }

    [Fact]
    public void Write_creates_medicine_children_with_factory_defaults()
    {
        var prescription = ValidPrescription(lines: [ValidLine(dosage: "  10mg  ")]);
        var medicine = prescription.Medicines.Single();

        medicine.PatientId.Should().Be(prescription.PatientId);
        medicine.MedicineId.Should().Be(3);
        medicine.Dosage.Should().Be("10mg");
        medicine.FrequencyInHours.Should().Be(8);
        medicine.DurationInDays.Should().Be(5);
        medicine.AddedDate.Should().Be(FixedDateTimeProvider.Instance.UtcNow.UtcDateTime);
        medicine.IsPrescribed.Should().BeTrue();
    }

    [Fact]
    public void Write_rejects_empty_medicine_list()
    {
        Action act = () => ValidPrescription(lines: []);

        act.Should().Throw<DomainException>()
            .WithMessage("Prescription must include at least one medicine.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Write_rejects_invalid_patient_id(int patientId)
    {
        Action act = () => ValidPrescription(patientId: patientId);

        act.Should().Throw<DomainException>()
            .WithMessage("Patient id must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Write_rejects_invalid_encounter_id(int encounterId)
    {
        Action act = () => ValidPrescription(encounterId: encounterId);

        act.Should().Throw<DomainException>()
            .WithMessage("Encounter id must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Write_rejects_medicine_with_invalid_id(int medicineId)
    {
        Action act = () => ValidPrescription(lines: [ValidLine(medicineId: medicineId)]);

        act.Should().Throw<DomainException>()
            .WithMessage("Medicine id must be greater than zero.");
    }

    [Fact]
    public void Write_rejects_medicine_with_whitespace_dosage()
    {
        Action act = () => ValidPrescription(lines: [ValidLine(dosage: "   ")]);

        act.Should().Throw<DomainException>()
            .WithMessage("Dosage is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Write_rejects_medicine_with_non_positive_frequency(int frequencyInHours)
    {
        Action act = () => ValidPrescription(lines: [ValidLine(frequencyInHours: frequencyInHours)]);

        act.Should().Throw<DomainException>()
            .WithMessage("Frequency in hours must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Write_rejects_medicine_with_non_positive_duration(int durationInDays)
    {
        Action act = () => ValidPrescription(lines: [ValidLine(durationInDays: durationInDays)]);

        act.Should().Throw<DomainException>()
            .WithMessage("Duration in days must be greater than zero.");
    }

    [Fact]
    public void Write_generates_name_identifier_with_patient_and_timestamp()
    {
        var prescription = ValidPrescription(patientId: 42);

        prescription.NameIdentifier.Should().Be("PRES-42-20260101000000");
    }

    private static Prescription ValidPrescription(
        int patientId = 1,
        int encounterId = 2,
        string? publisher = " Provider_Zero ",
        IEnumerable<PrescriptionMedicineLine>? lines = null)
    {
        return Prescription.Write(
            patientId,
            encounterId,
            publisher,
            lines ?? [ValidLine()],
            FixedDateTimeProvider.Instance);
    }

    private static PrescriptionMedicineLine ValidLine(
        int medicineId = 3,
        string dosage = "10mg",
        int frequencyInHours = 8,
        int durationInDays = 5)
    {
        return new PrescriptionMedicineLine(medicineId, dosage, frequencyInHours, durationInDays);
    }
}
