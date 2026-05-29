using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Prescription;

public sealed class MedicineNotFoundForPrescriptionException : DomainException
{
    public MedicineNotFoundForPrescriptionException(IEnumerable<int> medicineIds)
        : base($"One or more medicine ids were not found for prescription: {string.Join(", ", medicineIds)}.")
    {
        MedicineIds = medicineIds.ToArray();
    }

    public IReadOnlyCollection<int> MedicineIds { get; }
}
