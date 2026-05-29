using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Prescription;

public sealed class Prescription : AggregateRoot<int>
{
    private readonly List<PrescriptionMedicine> medicines = [];

    private Prescription()
    {
    }

    public int PatientId { get; private set; }
    public int? EncounterId { get; private set; }
    public string? Publisher { get; private set; }
    public string? PrescriptionImageUrl { get; private set; }
    public DateTime PrescriptionDate { get; private set; }
    public string NameIdentifier { get; private set; } = null!;
    public IReadOnlyCollection<PrescriptionMedicine> Medicines => medicines.AsReadOnly();

    public static Prescription Write(
        int patientId,
        int encounterId,
        string? publisher,
        IEnumerable<PrescriptionMedicineLine> medicineLines,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (patientId <= 0)
        {
            throw new DomainException("Patient id must be greater than zero.");
        }

        if (encounterId <= 0)
        {
            throw new DomainException("Encounter id must be greater than zero.");
        }

        if (medicineLines is null)
        {
            throw new DomainException("Prescription must include at least one medicine.");
        }

        var lines = medicineLines.ToArray();
        if (lines.Length == 0)
        {
            throw new DomainException("Prescription must include at least one medicine.");
        }

        var now = clock.UtcNow.UtcDateTime;
        var prescription = new Prescription
        {
            PatientId = patientId,
            EncounterId = encounterId,
            Publisher = NormalizeOptionalText(publisher),
            PrescriptionDate = now,
            NameIdentifier = GenerateNameIdentifier(patientId, now)
        };

        foreach (var line in lines)
        {
            prescription.medicines.Add(PrescriptionMedicine.Create(
                patientId,
                line.MedicineId,
                line.Dosage,
                line.FrequencyInHours,
                line.DurationInDays,
                clock));
        }

        return prescription;
    }

    public static Prescription UploadImage(
        int patientId,
        string? publisher,
        string prescriptionImageUrl,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (patientId <= 0)
        {
            throw new DomainException("Patient id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(prescriptionImageUrl))
        {
            throw new DomainException("Prescription image url is required.");
        }

        var now = clock.UtcNow.UtcDateTime;
        return new Prescription
        {
            PatientId = patientId,
            Publisher = NormalizeOptionalText(publisher),
            PrescriptionDate = now,
            PrescriptionImageUrl = prescriptionImageUrl.Trim(),
            NameIdentifier = GenerateNameIdentifier(patientId, now)
        };
    }

    private static string GenerateNameIdentifier(int patientId, DateTime dateTime)
    {
        return $"PRES-{patientId}-{dateTime:yyyyMMddHHmmss}";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
