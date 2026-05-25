using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.LabTestDto;
using HealthMate.Infrastructure.DTO.MedicalImageDto;
using HealthMate.Infrastructure.DTO.MedicineDto;
using HealthMate.Infrastructure.DTO.PrescriptionDto;
using HealthMate.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.HealthRecordRepos
{
	public class HealthRecordRepo : IHealthRecordRepo
	{
		private readonly HealthMateContext _context;
		private readonly IDbContextFactory<HealthMateContext> _contextFactory;
        public HealthRecordRepo(HealthMateContext context,IDbContextFactory<HealthMateContext> contextFactory)
        {
            _context = context;
			_contextFactory = contextFactory;
        }
        public async Task<(string Name, DateOnly BirthDate, float? Weight, float? Height, Gender Gender)> GetPatientGeneralInfoAsync(int patientId)
		{
			var result = await _context.Patients
				.Where(p => p.Patient_Id == patientId).Include(p => p.ApplicationUser).AsNoTracking()
				.Select(p => new
				{
					Name = p.ApplicationUser.First_Name + " " + p.ApplicationUser.Last_Name,
					p.BirthDate,
					p.Weight,
					p.Height,
					p.Gender
				})
				.FirstOrDefaultAsync();

			if (result == null)
				throw new Exception("Patient not found.");

			return (result.Name, result.BirthDate, result.Weight, result.Height, result.Gender);
		}


		#region HealthRecord Summary

		//return all labtests summary by patient Id
		public async Task<List<LabTestSummaryReadDto>> getLabTestsSummary(int patientId){
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.LabTests.Where(p => p.patientId == patientId).AsNoTracking().OrderByDescending(p => p.RecordedTime).Select(p => new LabTestSummaryReadDto{
				LabTestId = p.LabTestId,
				TestName = p.LabTestName,
				TestDate = p.RecordedTime.ToString("yyyy-MM-dd"),
				Note = p.Note ?? "No Notes Available",
				Result = "Normal"
			}).ToListAsync();
			return result;
		}

		//return all imaging summary

		public async Task<List<MedicalImageSummaryReadDto>> getMedicalImagesSummary(int patientId){
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.MedicalImages.Where(p => p.paitentId == patientId).AsNoTracking().OrderByDescending(p => p.TimeRecorded).Select(p => new MedicalImageSummaryReadDto{
				MedicalImageId = p.MedicalImageId,
				MedicalImageName = p.MedicalImageName,
				MedicalImageDate = p.TimeRecorded.ToShortDateString() // i should add date in the medical image 
			}).ToListAsync();

			return result;
		}

		//return all medicine Summary
		public async Task<List<MedicineSummaryReadDto>> getMedicineSummary(int patientId){
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.PatientMedicines.Where(p => p.PatientId == patientId).AsNoTracking().OrderByDescending(p => p.AddedDate).Select(p => new MedicineSummaryReadDto{
				Name = p.Medicine.Name,
				Date = p.AddedDate.ToString("yyyy-MM-dd"),
				DosePerTime = p.Dosage,
				DurationInDays = p.DurationInDays,
				FrequencyInHours = p.FrequencyInHours,
				patientMedicineId = p.PatientMedicineId,
				isOngoing = (DateTime.UtcNow - p.AddedDate).TotalDays <= p.DurationInDays
			}).ToListAsync();

			return result;
		}

		//return all Condition Summary
		public async Task<List<ConditionSummaryReadDto>> getConditionsSummary(int patientId){
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.Conditions.Where(p => p.PaientId == patientId).AsNoTracking().OrderByDescending(p => p.DateRecorded).Select(p => new ConditionSummaryReadDto{
				ConditionId = p.Condition_Id,
				ConditionName = p.Disease.Display_Name,
				Date = p.DateRecorded.ToString("yyyy-MM-dd"),
				Note = p.Note ?? "No Note Added",
				Severity = p.Severity.ToString(),
			}).ToListAsync();

			return result;
		}

		//return all prescription Summary
		public async Task<List<PrescriptionSummaryReadDto>> getPrescriptionsSummary(int patientId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.Prescriptions
				.Where(p => p.PatientId == patientId)
				.AsNoTracking().OrderByDescending(p => p.PrescriptionDate)
				.Select(p => new PrescriptionSummaryReadDto
				{
					PrescriptionId = p.PrescriptionId,
					PrescriptionDate = p.PrescriptionDate.ToString("yyyy-MM-dd"),
					ConditionName = p.Encounter != null 
						? p.Encounter.Conditions
							.Select(c => c.Disease.Display_Name)
							.FirstOrDefault() ?? "Unknown"
						: "Unknown", 
					Publisher = p.Publisher ?? "Unkown"
				})
				.ToListAsync();

			return result;
		}

		// return all encounter summary 
		public async Task<List<EncounterSumaryReadDto>> getEncounterSummary(int patientId){
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.Encounters.Where(p => p.PatientId == patientId).AsNoTracking().OrderByDescending(p => p.EndDate)
			.Select(p => new EncounterSumaryReadDto{
				EncounterId = p.Encounter_Id,
				EncounterDate = p.EndDate.ToString("yyyy-MM-dd"),
				ConditionName = p.Conditions.FirstOrDefault() != null 
                ? p.Conditions.FirstOrDefault().Disease.Display_Name 
                : "Unknown"
			}).ToListAsync();

			return result;
		}

		#endregion

		// return HealthRecord Details
		#region HealthRecord Details
		//MedicalImage
		public async Task<MedicalImageDetailsReadDto> getMedicalImageDetails(int MedicalImageId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.MedicalImages
				.Where(m => m.MedicalImageId == MedicalImageId)
				.AsNoTracking()
				.Select(m => new MedicalImageDetailsReadDto
				{
					PatientNationalId = m.patient.NationalId,
					PatientName = m.patient.ApplicationUser.First_Name + " " +m.patient.ApplicationUser.Last_Name,
					Interpretation = m.Interpertation,
					Date = m.TimeRecorded.ToString("yyyy-MM-dd") ?? "No Data",
					imageUrl = m.MedicalImageUrl,
					
					MedicalImageName = m.MedicalImageName,
				})
				.FirstOrDefaultAsync();

			return result;
		}

		//Prescription
		public async Task<PrescriptionDetailsReadDto> getPrescriptionDetails(int prescriptionId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.Prescriptions
				.Where(p => p.PrescriptionId == prescriptionId)
				.AsNoTracking()
				.Select(p => new PrescriptionDetailsReadDto
				{
					PatientNationalId = p.Encounter.Patient.NationalId,
					PatientName = p.Encounter.Patient.ApplicationUser.First_Name + " " + p.Encounter.Patient.ApplicationUser.Last_Name,
					PrescriptionDate = p.Encounter.EndDate.ToString("yyyy-MM-dd"),
					DiseaseName = p.Encounter.Conditions.FirstOrDefault() != null 
						? p.Encounter.Conditions.FirstOrDefault().Disease.Display_Name 
						: "Unknown",
					Medicines = p.PatientMedicines.Select(m => new MedicineDetailsReadDto
					{
						Name = m.Medicine.Name,
						FrequencyInHours = m.FrequencyInHours,
						DurationInDays = m.DurationInDays,
						Dose = m.Dosage
					}).ToList()
				})
				.FirstOrDefaultAsync();

			return result;
		}

		public async Task<LabTestDetailsReadDto> getLabTestDetails(int labTestId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.LabTests
				.Where(l => l.LabTestId == labTestId)
				.AsNoTracking()
				.Select(l => new LabTestDetailsReadDto
				{
					PatientNationalId = l.patient.NationalId,
					PatientName = l.patient.ApplicationUser.First_Name + " " + l.patient.ApplicationUser.Last_Name,
					LabTestName = l.LabTestName,
					LabTestDate = l.RecordedTime.ToString("yyyy-MM-dd"),
					LabTestImageUrl = l.ImageUrl,
					Results = l.LabTestResults.Select(r => new LabTestResultDto
					{
						AttributeName = r.LabTestAttribute.Name,
						Value = r.Value
					}).ToList()
				})
				.FirstOrDefaultAsync();

			return result;
		}
		// Condition Details
		private async Task<ConditionDetailsReadDto> getConditionDetails(int conditionId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var condition = await context.Conditions
				.Where(c => c.Condition_Id == conditionId)
				.Select(c => new ConditionDetailsReadDto
				{
					DiseaseName = c.Disease.Display_Name,
					PaientId = c.Patient.Patient_Id,
					DateRecorded = c.DateRecorded.ToString("yyyy-MM-dd HH:mm"),
					ClinicalStatus = c.ClinicalStatus.ToString(),
					Recorder = c.Recorder.ToString(),
					Severity = c.Severity.ToString(),
					Note = c.Note
				})
				.FirstOrDefaultAsync();

			if (condition == null)
				throw new Exception("Condition not found");

			return condition;
		}
		// Encounter Details
		public async Task<EncounterDetailsDto> getEncounterDetails(int encounterId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var encounter = await context.Encounters
				.Where(e => e.Encounter_Id == encounterId)
				.AsNoTracking()
				.Select(e => new 
				{
					patientId = e.Patient.Patient_Id,
					PatientNationalId = e.Patient.NationalId,
					PatientName = e.Patient.ApplicationUser.FullName,
					HealthCareProvidersName = e.HealthCareProvider.ApplicationUser.FullName,
					EncounterDate = e.StartDate,
					ReasonToVisit = e.Reason_To_Visit,
					TreatmentPlan = e.Treatment_Plan,
					Note = e.Note,
					Conditions = e.Conditions.Select(c => c.Condition_Id).ToList(),
					Medicines = e.Prescriptions
						.SelectMany(p => p.PatientMedicines)
						.Select(m => new MedicineDetailsReadDto
						{
							Name = m.Medicine.Name,
							FrequencyInHours = m.FrequencyInHours,
							DurationInDays = m.DurationInDays,
							Dose = m.Dosage
						})
						.ToList()
				})
				.FirstOrDefaultAsync();

			if (encounter == null)
				throw new Exception("Encounter not found");

			var conditionDetails = new List<ConditionDetailsReadDto>();
			foreach (var conditionId in encounter.Conditions)
			{
				var condition = await getConditionDetails(conditionId);
				conditionDetails.Add(condition);
			}

			return new EncounterDetailsDto
			{
				PatientNationalId = encounter.PatientNationalId,
				PatientName = encounter.PatientName,
				HealthCareProviderName = encounter.HealthCareProvidersName,
				Date = encounter.EncounterDate.ToString("yyyy-MM-dd"),
				Reason_To_Visit = encounter.ReasonToVisit,
				Treatment_Plan = encounter.TreatmentPlan,
				Note = encounter.Note ?? "",
				Conditions = conditionDetails,
				Prescription = encounter.Medicines
			};
		}

		
		#endregion
	}
}
