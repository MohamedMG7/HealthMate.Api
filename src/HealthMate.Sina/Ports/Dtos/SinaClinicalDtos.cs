namespace HealthMate.Sina.Ports;

public record PatientChartSummary(
    int PatientId,
    string RecordId,
    string Gender,
    int Age,
    string Governorate,
    decimal? Bmi,
    IReadOnlyList<ConditionSummary> ActiveConditions,
    IReadOnlyList<AllergySummary> Allergies,
    IReadOnlyList<ActiveMedicationSummary> CurrentMedications,
    IReadOnlyList<EncounterSummary> RecentEncounters,
    IReadOnlyList<LabResultSummary> RecentAbnormalLabs);

public record ConditionSummary(
    int Id,
    string RecordId,
    string Name,
    string Severity,
    DateTime RecordedAt);

public record AllergySummary(
    int Id,
    string RecordId,
    string Substance,
    string Severity,
    string? Reaction,
    string? Notes,
    DateTime RecordedAt);

public record ActiveMedicationSummary(
    int PatientMedicineId,
    int MedicineId,
    string RecordId,
    string MedicineName,
    string? ActiveIngredients,
    string Dosage,
    int FrequencyInHours,
    int DurationInDays,
    DateTime AddedDate);

public record EncounterSummary(
    int Id,
    string RecordId,
    DateTime Start,
    DateTime End,
    string Reason,
    string? TreatmentPlan,
    string? Note);

public record LabTestSummary(
    int Id,
    string RecordId,
    string Name,
    DateTime RecordedAt,
    string? Note,
    IReadOnlyList<LabResultSummary> Results);

public record LabResultSummary(
    int LabTestId,
    int ResultId,
    string RecordId,
    string Name,
    string? Abbreviation,
    decimal Value,
    string Unit,
    string NormalRange,
    string Abnormality,
    DateTime RecordedAt);

public record ObservationSummary(
    int Id,
    string RecordId,
    string? Code,
    string? Display,
    decimal? Value,
    string? Unit,
    string? Interpretation,
    DateTime ObservedAt);

public record PrescriptionSummary(
    int Id,
    string RecordId,
    DateTime Date,
    int? EncounterId,
    IReadOnlyList<PrescriptionMedicationSummary> Medicines);

public record PrescriptionMedicationSummary(
    int PatientMedicineId,
    int MedicineId,
    string MedicineName,
    string? ActiveIngredients,
    string Dosage,
    int FrequencyInHours,
    int DurationInDays);
