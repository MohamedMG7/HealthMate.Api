using HealthMate.Domain.Common.Enums;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.HealthRecord.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Prescriptions.Contracts.Medicines;
using HealthMate.Application.Prescriptions.Contracts;
using HealthMate.Domain.Aggregates.Condition;
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
				.Where(p => p.Id == patientId).AsNoTracking()
				.Select(p => new
				{
					p.ApplicationUserId,
					p.BirthDate,
					p.Weight,
					p.Height,
					p.Gender
				})
				.FirstOrDefaultAsync();

			if (result == null)
				throw new Exception("Patient not found.");

			var name = await GetUserFullNameAsync(_context, result.ApplicationUserId);
			return (name, result.BirthDate, result.Weight, result.Height, result.Gender);
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

			var result = await (
				from patientMedicine in context.PrescriptionMedicines.AsNoTracking()
				join medicine in context.Medicines.AsNoTracking() on patientMedicine.MedicineId equals medicine.Id
				where patientMedicine.PatientId == patientId
				orderby patientMedicine.AddedDate descending
				select new MedicineSummaryReadDto{
					Name = medicine.Name,
					Date = patientMedicine.AddedDate.ToString("yyyy-MM-dd"),
					DosePerTime = patientMedicine.Dosage,
					DurationInDays = patientMedicine.DurationInDays,
					FrequencyInHours = patientMedicine.FrequencyInHours,
					patientMedicineId = patientMedicine.Id,
					isOngoing = (DateTime.UtcNow - patientMedicine.AddedDate).TotalDays <= patientMedicine.DurationInDays
				})
				.ToListAsync();

			return result;
		}

		//return all Condition Summary
		public async Task<List<ConditionSummaryReadDto>> getConditionsSummary(int patientId){
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await (
				from condition in context.Conditions.AsNoTracking()
				join disease in context.Diseases.AsNoTracking() on condition.DiseaseId equals disease.Disease_Id
				where condition.PatientId == patientId
				orderby condition.DateRecorded descending
				select new ConditionSummaryReadDto{
					ConditionId = condition.Id,
					ConditionName = disease.Display_Name,
					Date = condition.DateRecorded.ToString("yyyy-MM-dd"),
					Note = condition.Note ?? "No Note Added",
					Severity = condition.Severity.ToString(),
				})
				.ToListAsync();

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
					PrescriptionId = p.Id,
					PrescriptionDate = p.PrescriptionDate.ToString("yyyy-MM-dd"),
					ConditionName = p.EncounterId.HasValue
						? context.Conditions
							.Join(
								context.Diseases,
								condition => condition.DiseaseId,
								disease => disease.Disease_Id,
								(condition, disease) => new { condition, disease })
							.Where(row => row.condition.EncounterId == p.EncounterId)
							.Select(row => row.disease.Display_Name)
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
				EncounterId = p.Id,
				EncounterDate = p.EndDate.ToString("yyyy-MM-dd"),
				ConditionName = context.Conditions
					.Join(
						context.Diseases,
						condition => condition.DiseaseId,
						disease => disease.Disease_Id,
						(condition, disease) => new { condition, disease })
					.Where(row => row.condition.EncounterId == p.Id)
					.Select(row => row.disease.Display_Name)
					.FirstOrDefault() ?? "Unknown"
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
				.Select(m => new
				{
					PatientNationalId = m.patient.NationalId,
					PatientUserId = m.patient.ApplicationUserId,
					m.Interpertation,
					m.TimeRecorded,
					m.MedicalImageUrl,
					
					m.MedicalImageName,
				})
				.FirstOrDefaultAsync();

			return result is null
				? null
				: new MedicalImageDetailsReadDto
				{
					PatientNationalId = result.PatientNationalId.Value,
					PatientName = await GetUserFullNameAsync(context, result.PatientUserId),
					Interpretation = result.Interpertation,
					Date = result.TimeRecorded.ToString("yyyy-MM-dd") ?? "No Data",
					imageUrl = result.MedicalImageUrl,
					MedicalImageName = result.MedicalImageName,
				};
		}

		//Prescription
		public async Task<PrescriptionDetailsReadDto> getPrescriptionDetails(int prescriptionId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.Prescriptions
				.Where(p => p.Id == prescriptionId)
				.AsNoTracking()
				.Select(p => new
				{
					PatientNationalId = context.Patients
						.Where(patient => patient.Id == p.PatientId)
						.Select(patient => patient.NationalId)
						.FirstOrDefault(),
					PatientUserId = context.Patients
						.Where(patient => patient.Id == p.PatientId)
						.Select(patient => patient.ApplicationUserId)
						.FirstOrDefault(),
					PrescriptionDate = p.EncounterId.HasValue
						? context.Encounters
							.Where(encounter => encounter.Id == p.EncounterId.Value)
							.Select(encounter => encounter.EndDate)
							.FirstOrDefault()
						: p.PrescriptionDate,
					DiseaseName = p.EncounterId.HasValue
						? context.Conditions
							.Join(
								context.Diseases,
								condition => condition.DiseaseId,
								disease => disease.Disease_Id,
								(condition, disease) => new { condition, disease })
							.Where(row => row.condition.EncounterId == p.EncounterId)
							.Select(row => row.disease.Display_Name)
							.FirstOrDefault() ?? "Unknown"
						: "Unknown",
					Medicines = p.Medicines.Select(m => new MedicineDetailsReadDto
					{
						Name = context.Medicines
							.Where(medicine => medicine.Id == m.MedicineId)
							.Select(medicine => medicine.Name)
							.FirstOrDefault() ?? "Unknown",
						FrequencyInHours = m.FrequencyInHours,
						DurationInDays = m.DurationInDays,
						Dose = m.Dosage
					}).ToList()
				})
				.FirstOrDefaultAsync();

			return result is null
				? null
				: new PrescriptionDetailsReadDto
				{
					PatientNationalId = result.PatientNationalId.Value,
					PatientName = await GetUserFullNameAsync(context, result.PatientUserId),
					PrescriptionDate = result.PrescriptionDate.ToString("yyyy-MM-dd"),
					DiseaseName = result.DiseaseName,
					Medicines = result.Medicines
				};
		}

		public async Task<LabTestDetailsReadDto> getLabTestDetails(int labTestId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var result = await context.LabTests
				.Where(l => l.LabTestId == labTestId)
				.AsNoTracking()
				.Select(l => new
				{
					PatientNationalId = l.patient.NationalId,
					PatientUserId = l.patient.ApplicationUserId,
					l.LabTestName,
					l.RecordedTime,
					l.ImageUrl,
					Results = l.LabTestResults.Select(r => new LabTestResultDto
					{
						AttributeName = r.LabTestAttribute.Name,
						Value = r.Value
					}).ToList()
				})
				.FirstOrDefaultAsync();

			return result is null
				? null
				: new LabTestDetailsReadDto
				{
					PatientNationalId = result.PatientNationalId.Value,
					PatientName = await GetUserFullNameAsync(context, result.PatientUserId),
					LabTestName = result.LabTestName,
					LabTestDate = result.RecordedTime.ToString("yyyy-MM-dd"),
					LabTestImageUrl = result.ImageUrl,
					Results = result.Results
				};
		}
		// Condition Details
		private async Task<ConditionDetailsReadDto> getConditionDetails(int conditionId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var condition = await (
				from c in context.Conditions.AsNoTracking()
				join disease in context.Diseases.AsNoTracking() on c.DiseaseId equals disease.Disease_Id
				where c.Id == conditionId
				select new
				{
					DiseaseName = disease.Display_Name,
					c.PatientId,
					c.DateRecorded,
					c.ClinicalStatus,
					Recorder = EF.Property<ConditionRecorder>(c, "Recorder"),
					c.Severity,
					c.Note
				})
				.FirstOrDefaultAsync();

			if (condition == null)
				throw new Exception("Condition not found");

			return new ConditionDetailsReadDto
			{
				DiseaseName = condition.DiseaseName,
				PaientId = condition.PatientId,
				DateRecorded = condition.DateRecorded.ToString("yyyy-MM-dd HH:mm"),
				ClinicalStatus = condition.ClinicalStatus.ToString(),
				Recorder = condition.Recorder.ToString(),
				Severity = condition.Severity.ToString(),
				Note = condition.Note
			};
		}
		// Encounter Details
		public async Task<EncounterDetailsDto> getEncounterDetails(int encounterId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var encounter = await context.Encounters
				.AsNoTracking()
				.Where(e => e.Id == encounterId)
				.FirstOrDefaultAsync();

			if (encounter == null)
				throw new Exception("Encounter not found");

			var patient = await context.Patients
				.AsNoTracking()
				.Where(p => p.Id == encounter.PatientId)
				.Select(p => new
				{
					p.NationalId,
					p.ApplicationUserId
				})
				.FirstOrDefaultAsync();

			var healthCareProviderName = await context.HealthCareProviders
				.AsNoTracking()
				.Where(provider => provider.HealthCareProvider_Id == encounter.HealthCareProviderId)
				.Select(provider => provider.ApplicationUser.First_Name + " " + provider.ApplicationUser.Last_Name)
				.FirstOrDefaultAsync() ?? "No Data";

			var conditionIds = await context.Conditions
				.AsNoTracking()
				.Where(condition => condition.EncounterId == encounter.Id)
				.Select(condition => condition.Id)
				.ToListAsync();

			var medicines = await context.Prescriptions
				.AsNoTracking()
				.Where(prescription => prescription.EncounterId == encounter.Id)
				.SelectMany(prescription => prescription.Medicines)
				.Select(medicine => new MedicineDetailsReadDto
				{
					Name = context.Medicines
						.Where(referenceMedicine => referenceMedicine.Id == medicine.MedicineId)
						.Select(referenceMedicine => referenceMedicine.Name)
						.FirstOrDefault() ?? "Unknown",
					FrequencyInHours = medicine.FrequencyInHours,
					DurationInDays = medicine.DurationInDays,
					Dose = medicine.Dosage
				})
				.ToListAsync();

			var conditionDetails = new List<ConditionDetailsReadDto>();
			foreach (var conditionId in conditionIds)
			{
				var condition = await getConditionDetails(conditionId);
				conditionDetails.Add(condition);
			}

			return new EncounterDetailsDto
			{
				PatientNationalId = patient?.NationalId.Value ?? "No Data",
				PatientName = await GetUserFullNameAsync(context, patient?.ApplicationUserId),
				HealthCareProviderName = healthCareProviderName,
				Date = encounter.StartDate.ToString("yyyy-MM-dd"),
				Reason_To_Visit = encounter.ReasonToVisit.Value,
				Treatment_Plan = encounter.TreatmentPlan,
				Note = encounter.Note ?? "",
				Conditions = conditionDetails,
				Prescription = medicines
			};
		}

		private static async Task<string> GetUserFullNameAsync(HealthMateContext context, string? applicationUserId)
		{
			if (string.IsNullOrWhiteSpace(applicationUserId))
			{
				return "No Data";
			}

			var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicationUserId);
			return user is null ? "No Data" : user.First_Name + " " + user.Last_Name;
		}


		#endregion

		public Task<HealthRecordsReadDto> GetAllHealthRecordsAsync(int patientId)
		{
			var encounters = _context.Encounters.AsNoTracking().Where(p => p.PatientId == patientId).ToList();
			var conditions = (
				from condition in _context.Conditions.AsNoTracking()
				join disease in _context.Diseases on condition.DiseaseId equals disease.Disease_Id
				where condition.PatientId == patientId
				select new
				{
					condition.PatientId,
					condition.FhirId,
					Recorder = EF.Property<ConditionRecorder>(condition, "Recorder"),
					condition.ClinicalStatus,
					condition.Severity,
					condition.DateRecorded,
					condition.Id,
					BodySiteId = EF.Property<int?>(condition, "BodySiteId"),
					condition.EncounterId,
					DiseaseName = disease.Display_Name,
					condition.Note
				})
				.ToList();
			var observations = _context.Observations.AsNoTracking().Where(p => p.PatientId == patientId).ToList();
			var patientIds = observations
				.Select(o => o.PatientId)
				.Distinct()
				.ToArray();
			var patients = _context.Patients
				.Where(patient => patientIds.Contains(patient.Id))
				.ToDictionary(patient => patient.Id);
			var patientUserIds = observations
				.Select(o => patients.TryGetValue(o.PatientId, out var patient) ? patient.ApplicationUserId : null)
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct()
				.ToArray();
			var patientUsers = _context.Users
				.Where(user => patientUserIds.Contains(user.Id))
				.ToDictionary(user => user.Id);
			var bodySiteIds = observations
				.Select(o => o.BodySiteId)
				.OfType<int>()
				.Distinct()
				.ToArray();
			var bodySites = _context.BodySites
				.Where(bodySite => bodySiteIds.Contains(bodySite.BodySite_Id))
				.ToDictionary(bodySite => bodySite.BodySite_Id);

			var encounterList = encounters.Select(x => new EncounterReadDto
			{
				Encounter_Id = x.Id,
				Patient_Id = x.PatientId,
				Encounter_Fhir_Id = x.FhirId,
				HealthcareProvider_Id = x.HealthCareProviderId,
				isDeleted = x.IsDeleted,
				Reason_To_Visit = x.ReasonToVisit.Value,
				StartDate = x.StartDate,
				EndDate = x.EndDate,
				Location = x.Location,
				Treatment_Plan = x.TreatmentPlan,
				Note = x.Note
			});

			var conditionList = conditions.Select(x => new ConditionReadDto
			{
				PaientId = x.PatientId,
				Condition_Fhir_Id = x.FhirId,
				Recorder = (HealthMate.Application.Abstractions.Enums.Recorder)(int)x.Recorder,
				ClinicalStatus = x.ClinicalStatus,
				Severity = x.Severity,
				DateRecorded = x.DateRecorded,
				Condition_Id = x.Id,
				BodySiteId = x.BodySiteId,
				EncounterId = x.EncounterId,
				DiseaseName = x.DiseaseName,
				Note = x.Note
			});

			var observationList = observations.Select(x => new ObservationReadDto
			{
				Observation_Id = x.Id,
				Observation_Fhir_Id = x.FhirId,
				Category = x.Category,
				Code = x.Code,
				CodeDisplayName = x.CodeDisplayName,
				DateOfObservation = x.DateOfObservation,
				Interpertation = x.Interpretation,
				ValueQuanitity = x.ValueQuantity,
				ValueUnit = x.ValueUnit,
				PatientName = patients.TryGetValue(x.PatientId, out var patient) && patient.ApplicationUserId is not null && patientUsers.TryGetValue(patient.ApplicationUserId, out var user) ? user.First_Name : "No Data",
				BodySiteName = x.BodySiteId.HasValue && bodySites.TryGetValue(x.BodySiteId.Value, out var bodySite) ? bodySite.DisplayName : "No Data",
			});

			return Task.FromResult(new HealthRecordsReadDto
			{
				Conditions = conditionList,
				Encounters = encounterList,
				Observations = observationList
			});
		}
	}
}
