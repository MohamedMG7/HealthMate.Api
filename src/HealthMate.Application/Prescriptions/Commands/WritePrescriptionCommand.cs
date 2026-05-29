using HealthMate.Application.Common;

namespace HealthMate.Application.Prescriptions.Commands;

public sealed record WritePrescriptionCommand(
    int EncounterId,
    string? Publisher,
    IReadOnlyList<WritePrescriptionMedicineLine> Medicines) : ICommand<WritePrescriptionResult>;

public sealed record WritePrescriptionMedicineLine(
    int MedicineId,
    string Dosage,
    int FrequencyInHours,
    int DurationInDays);

public sealed record WritePrescriptionResult(
    int PrescriptionId,
    int PatientId,
    int MedicineCount);
