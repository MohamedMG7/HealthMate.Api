using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Tools;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Sina;

public class InProcessSinaClinicalReader : ISinaClinicalReader
{
    private readonly HealthMateContext context;
    private readonly ISinaClock clock;

    public InProcessSinaClinicalReader(HealthMateContext context, ISinaClock clock)
    {
        this.context = context;
        this.clock = clock;
    }

    public async Task<PatientChartSummary?> GetPatientChartAsync(int patientId, CancellationToken ct)
    {
        var patient = await context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is null)
        {
            return null;
        }

        var conditions = await (
            from c in context.Conditions.AsNoTracking()
            join d in context.Diseases.AsNoTracking() on c.DiseaseId equals d.Disease_Id
            where c.PatientId == patientId && c.ClinicalStatus == ClinicalStatus.Active
            orderby c.DateRecorded descending
            select new ConditionSummary(
                c.Id,
                $"#C-{c.Id}",
                d.Display_Name,
                c.Severity.ToString(),
                c.DateRecorded))
            .Take(10)
            .ToArrayAsync(ct);

        var allergies = await GetActiveAllergiesAsync(patientId, ct);
        var medications = await GetActiveMedicationsAsync(patientId, ct);
        var encounters = await context.Encounters
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && !e.IsDeleted)
            .OrderByDescending(e => e.StartDate)
            .Take(3)
            .Select(e => new EncounterSummary(
                e.Id,
                $"#E-{e.Id}",
                e.StartDate,
                e.EndDate,
                e.ReasonToVisit.Value,
                e.TreatmentPlan,
                e.Note))
            .ToArrayAsync(ct);

        var abnormalLabs = await GetRecentAbnormalLabsAsync(patientId, 90, ct);

        return new PatientChartSummary(
            patient.Id,
            $"#P-{patient.Id}",
            patient.Gender.ToString(),
            CalculateAge(patient.BirthDate),
            patient.Governorate.Value,
            CalculateBmi(patient.Weight, patient.Height),
            conditions,
            allergies,
            medications,
            encounters,
            abnormalLabs);
    }

    public async Task<LabTestSummary?> GetLabTestAsync(int patientId, int labTestId, CancellationToken ct)
    {
        var lab = await context.LabTests
            .AsNoTracking()
            .Include(l => l.LabTestResults!)
            .ThenInclude(r => r.LabTestAttribute)
            .FirstOrDefaultAsync(l => l.patientId == patientId && l.LabTestId == labTestId, ct);

        return lab is null ? null : MapLabTest(lab);
    }

    public async Task<IReadOnlyList<ObservationSummary>> SearchObservationsAsync(int patientId, string codeOrDisplay, DateTime? from, DateTime? to, int limit, CancellationToken ct)
    {
        var term = codeOrDisplay.Trim();
        var query = context.Observations.AsNoTracking().Where(o => o.PatientId == patientId && !o.IsDeleted);
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(o =>
                (o.Code != null && EF.Functions.ILike(o.Code, $"%{term}%"))
                || (o.CodeDisplayName != null && EF.Functions.ILike(o.CodeDisplayName, $"%{term}%")));
        }

        if (from.HasValue)
        {
            query = query.Where(o => o.DateOfObservation >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.DateOfObservation <= to.Value);
        }

        return await query
            .OrderByDescending(o => o.DateOfObservation)
            .Take(limit)
            .Select(o => new ObservationSummary(
                o.Id,
                $"#O-{o.Id}",
                o.Code,
                o.CodeDisplayName,
                o.ValueQuantity,
                o.ValueUnit,
                o.Interpretation,
                o.DateOfObservation))
            .ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<PrescriptionSummary>> GetPrescriptionHistoryAsync(int patientId, string? medicineName, CancellationToken ct)
    {
        var query = context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Medicines)
            .Where(p => p.PatientId == patientId);

        if (!string.IsNullOrWhiteSpace(medicineName))
        {
            var term = medicineName.Trim();
            query = query.Where(p => p.Medicines.Any(pm => context.Medicines.Any(medicine =>
                medicine.Id == pm.MedicineId && EF.Functions.ILike(medicine.Name, $"%{term}%"))));
        }

        var prescriptions = await query.OrderByDescending(p => p.PrescriptionDate).Take(20).ToArrayAsync(ct);
        var medicineIds = prescriptions
            .SelectMany(prescription => prescription.Medicines)
            .Select(medicine => medicine.MedicineId)
            .Distinct()
            .ToArray();
        var medicinesById = await context.Medicines
            .AsNoTracking()
            .Where(medicine => medicineIds.Contains(medicine.Id))
            .ToDictionaryAsync(medicine => medicine.Id, ct);

        return prescriptions.Select(prescription => MapPrescription(prescription, medicinesById)).ToArray();
    }

    public async Task<EncounterSummary?> GetEncounterNoteAsync(int patientId, int encounterId, CancellationToken ct)
    {
        return await context.Encounters
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && e.Id == encounterId && !e.IsDeleted)
            .Select(e => new EncounterSummary(e.Id, $"#E-{e.Id}", e.StartDate, e.EndDate, e.ReasonToVisit.Value, e.TreatmentPlan, e.Note))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<AllergySummary>> GetActiveAllergiesAsync(int patientId, CancellationToken ct)
    {
        return await context.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientId == patientId && a.IsActive)
            .OrderByDescending(a => a.RecordedAt)
            .Select(a => new AllergySummary(a.Id, $"#A-{a.Id}", a.Substance, a.Severity.ToString(), a.Reaction, a.Notes, a.RecordedAt))
            .ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<ActiveMedicationSummary>> GetActiveMedicationsAsync(int patientId, CancellationToken ct)
    {
        var now = clock.UtcNow();
        var medications = await (
            from patientMedicine in context.PrescriptionMedicines.AsNoTracking()
            join medicine in context.Medicines.AsNoTracking() on patientMedicine.MedicineId equals medicine.Id
            where patientMedicine.PatientId == patientId
            select new { PatientMedicine = patientMedicine, Medicine = medicine })
            .ToArrayAsync(ct);

        return medications
            .Where(row => row.PatientMedicine.DurationInDays <= 0 || row.PatientMedicine.AddedDate.AddDays(row.PatientMedicine.DurationInDays) >= now)
            .Select(row => new ActiveMedicationSummary(
                row.PatientMedicine.Id,
                row.PatientMedicine.MedicineId,
                $"#PM-{row.PatientMedicine.Id}",
                row.Medicine.Name,
                row.Medicine.ActiveIngrediantes,
                row.PatientMedicine.Dosage,
                row.PatientMedicine.FrequencyInHours,
                row.PatientMedicine.DurationInDays,
                row.PatientMedicine.AddedDate))
            .ToArray();
    }

    private async Task<IReadOnlyList<LabResultSummary>> GetRecentAbnormalLabsAsync(int patientId, int days, CancellationToken ct)
    {
        var cutoff = clock.UtcNow().AddDays(-days);
        var labs = await context.LabTests
            .AsNoTracking()
            .Include(l => l.LabTestResults!)
            .ThenInclude(r => r.LabTestAttribute)
            .Where(l => l.patientId == patientId && l.RecordedTime >= cutoff)
            .OrderByDescending(l => l.RecordedTime)
            .Take(30)
            .ToArrayAsync(ct);

        return labs
            .SelectMany(l => MapLabTest(l).Results)
            .Where(r => r.Abnormality is "high" or "low")
            .Take(20)
            .ToArray();
    }

    private static LabTestSummary MapLabTest(LabTest lab)
    {
        var results = (lab.LabTestResults ?? [])
            .Select(r => new LabResultSummary(
                lab.LabTestId,
                r.Id,
                $"#LR-{r.Id}",
                r.LabTestAttribute.Name,
                r.LabTestAttribute.Abbreviation,
                r.Value,
                r.LabTestAttribute.ValueUnit,
                r.LabTestAttribute.NormalRange,
                LabRangeParser.GetAbnormality(r.Value, r.LabTestAttribute.NormalRange),
                lab.RecordedTime))
            .ToArray();

        return new LabTestSummary(lab.LabTestId, $"#L-{lab.LabTestId}", lab.LabTestName, lab.RecordedTime, lab.Note, results);
    }

    private static PrescriptionSummary MapPrescription(Prescription prescription, IReadOnlyDictionary<int, Medicine> medicinesById)
    {
        var medicines = prescription.Medicines
            .Select(pm =>
            {
                medicinesById.TryGetValue(pm.MedicineId, out var medicine);
                return new PrescriptionMedicationSummary(
                    pm.Id,
                    pm.MedicineId,
                    medicine?.Name ?? "Unknown",
                    medicine?.ActiveIngrediantes,
                    pm.Dosage,
                    pm.FrequencyInHours,
                    pm.DurationInDays);
            })
            .ToArray();

        return new PrescriptionSummary(prescription.Id, $"#RX-{prescription.Id}", prescription.PrescriptionDate, prescription.EncounterId, medicines);
    }

    private int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow());
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    private static decimal? CalculateBmi(float? weightKg, float? height)
    {
        if (!weightKg.HasValue || !height.HasValue || weightKg.Value <= 0 || height.Value <= 0)
        {
            return null;
        }

        var heightMeters = height.Value > 3 ? (decimal)height.Value / 100m : (decimal)height.Value;
        if (heightMeters <= 0)
        {
            return null;
        }

        return Math.Round((decimal)weightKg.Value / (heightMeters * heightMeters), 1);
    }
}
