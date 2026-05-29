using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Prescription;

public sealed class PrescriptionMedicine : Entity<int>
{
    private PrescriptionMedicine()
    {
    }

    public int PatientId { get; private set; }
    public int? PrescriptionId { get; private set; }
    public int MedicineId { get; private set; }
    public string Dosage { get; private set; } = null!;
    public int FrequencyInHours { get; private set; }
    public int DurationInDays { get; private set; }
    public DateTime AddedDate { get; private set; }
    public bool IsPrescribed { get; private set; }

    internal static PrescriptionMedicine Create(
        int patientId,
        int medicineId,
        string dosage,
        int frequencyInHours,
        int durationInDays,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (patientId <= 0)
        {
            throw new DomainException("Patient id must be greater than zero.");
        }

        if (medicineId <= 0)
        {
            throw new DomainException("Medicine id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(dosage))
        {
            throw new DomainException("Dosage is required.");
        }

        if (frequencyInHours <= 0)
        {
            throw new DomainException("Frequency in hours must be greater than zero.");
        }

        if (durationInDays <= 0)
        {
            throw new DomainException("Duration in days must be greater than zero.");
        }

        return new PrescriptionMedicine
        {
            PatientId = patientId,
            MedicineId = medicineId,
            Dosage = dosage.Trim(),
            FrequencyInHours = frequencyInHours,
            DurationInDays = durationInDays,
            AddedDate = clock.UtcNow.UtcDateTime,
            IsPrescribed = true
        };
    }
}
