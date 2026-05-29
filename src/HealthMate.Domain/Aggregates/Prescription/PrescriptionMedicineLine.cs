namespace HealthMate.Domain.Aggregates.Prescription;

public sealed record PrescriptionMedicineLine(
    int MedicineId,
    string Dosage,
    int FrequencyInHours,
    int DurationInDays);
