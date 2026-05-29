namespace HealthMate.Application.Prescriptions.Contracts;

public sealed record WritePrescriptionRequestDto(
    string? Publisher,
    IReadOnlyList<WritePrescriptionMedicineDto> Medicines);

public sealed record WritePrescriptionMedicineDto(
    int MedicineId,
    string Dosage,
    int FrequencyInHours,
    int DurationInDays);
