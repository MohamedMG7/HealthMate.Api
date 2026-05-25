namespace HealthMate.Sina.Ports;

public interface ISinaClinicalReader
{
    Task<PatientChartSummary?> GetPatientChartAsync(int patientId, CancellationToken ct);
    Task<LabTestSummary?> GetLabTestAsync(int patientId, int labTestId, CancellationToken ct);
    Task<IReadOnlyList<ObservationSummary>> SearchObservationsAsync(int patientId, string codeOrDisplay, DateTime? from, DateTime? to, int limit, CancellationToken ct);
    Task<IReadOnlyList<PrescriptionSummary>> GetPrescriptionHistoryAsync(int patientId, string? medicineName, CancellationToken ct);
    Task<EncounterSummary?> GetEncounterNoteAsync(int patientId, int encounterId, CancellationToken ct);
    Task<IReadOnlyList<AllergySummary>> GetActiveAllergiesAsync(int patientId, CancellationToken ct);
    Task<IReadOnlyList<ActiveMedicationSummary>> GetActiveMedicationsAsync(int patientId, CancellationToken ct);
}
