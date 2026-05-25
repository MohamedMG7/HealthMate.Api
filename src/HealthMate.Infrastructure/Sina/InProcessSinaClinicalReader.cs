using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;
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
        var patient = await context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Patient_Id == patientId, ct);
        if (patient is null)
        {
            return null;
        }

        var conditions = await context.Conditions
            .AsNoTracking()
            .Include(c => c.Disease)
            .Where(c => c.PaientId == patientId && c.ClinicalStatus == ClinicalStatus.Active)
            .OrderByDescending(c => c.DateRecorded)
            .Take(10)
            .Select(c => new ConditionSummary(
                c.Condition_Id,
                $"#C-{c.Condition_Id}",
                c.Disease.Display_Name,
                c.Severity.ToString(),
                c.DateRecorded))
            .ToArrayAsync(ct);

        var allergies = await GetActiveAllergiesAsync(patientId, ct);
        var medications = await GetActiveMedicationsAsync(patientId, ct);
        var encounters = await context.Encounters
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && !e.isDeleted)
            .OrderByDescending(e => e.StartDate)
            .Take(3)
            .Select(e => new EncounterSummary(
                e.Encounter_Id,
                $"#E-{e.Encounter_Id}",
                e.StartDate,
                e.EndDate,
                e.Reason_To_Visit,
                e.Treatment_Plan,
                e.Note))
            .ToArrayAsync(ct);

        var abnormalLabs = await GetRecentAbnormalLabsAsync(patientId, 90, ct);

        return new PatientChartSummary(
            patient.Patient_Id,
            $"#P-{patient.Patient_Id}",
            patient.Gender.ToString(),
            CalculateAge(patient.BirthDate),
            patient.Governorate,
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
        var query = context.Observations.AsNoTracking().Where(o => o.PatientId == patientId && !o.isDeleted);
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
                o.Observation_Id,
                $"#O-{o.Observation_Id}",
                o.Code,
                o.CodeDisplayName,
                o.ValueQuanitity,
                o.ValueUnit,
                o.Interpertation,
                o.DateOfObservation))
            .ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<PrescriptionSummary>> GetPrescriptionHistoryAsync(int patientId, string? medicineName, CancellationToken ct)
    {
        var query = context.Prescriptions
            .AsNoTracking()
            .Include(p => p.PatientMedicines!)
            .ThenInclude(pm => pm.Medicine)
            .Where(p => p.PatientId == patientId);

        if (!string.IsNullOrWhiteSpace(medicineName))
        {
            query = query.Where(p => p.PatientMedicines!.Any(pm => EF.Functions.ILike(pm.Medicine.Name, $"%{medicineName}%")));
        }

        var prescriptions = await query.OrderByDescending(p => p.PrescriptionDate).Take(20).ToArrayAsync(ct);
        return prescriptions.Select(MapPrescription).ToArray();
    }

    public async Task<EncounterSummary?> GetEncounterNoteAsync(int patientId, int encounterId, CancellationToken ct)
    {
        return await context.Encounters
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && e.Encounter_Id == encounterId && !e.isDeleted)
            .Select(e => new EncounterSummary(e.Encounter_Id, $"#E-{e.Encounter_Id}", e.StartDate, e.EndDate, e.Reason_To_Visit, e.Treatment_Plan, e.Note))
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
        var medications = await context.PatientMedicines
            .AsNoTracking()
            .Include(pm => pm.Medicine)
            .Where(pm => pm.PatientId == patientId)
            .ToArrayAsync(ct);

        return medications
            .Where(pm => pm.DurationInDays <= 0 || pm.AddedDate.AddDays(pm.DurationInDays) >= now)
            .Select(pm => new ActiveMedicationSummary(
                pm.PatientMedicineId,
                pm.MedicineId,
                $"#PM-{pm.PatientMedicineId}",
                pm.Medicine.Name,
                pm.Medicine.ActiveIngrediantes,
                pm.Dosage,
                pm.FrequencyInHours,
                pm.DurationInDays,
                pm.AddedDate))
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

    private static PrescriptionSummary MapPrescription(Prescription prescription)
    {
        var medicines = (prescription.PatientMedicines ?? [])
            .Select(pm => new PrescriptionMedicationSummary(
                pm.PatientMedicineId,
                pm.MedicineId,
                pm.Medicine.Name,
                pm.Medicine.ActiveIngrediantes,
                pm.Dosage,
                pm.FrequencyInHours,
                pm.DurationInDays))
            .ToArray();

        return new PrescriptionSummary(prescription.PrescriptionId, $"#RX-{prescription.PrescriptionId}", prescription.PrescriptionDate, prescription.EncounterId, medicines);
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
