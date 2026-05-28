using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.HealthRecord.Contracts;
using HealthMate.Application.Observations.Contracts;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.HealthRecordRepos;
using Microsoft.EntityFrameworkCore;
using HealthMate.Application.Prescriptions.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Manager.UtilityManager;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Infrastructure.Data.DbHelper;

namespace HealthMate.Application.Manager.HealthRecordManager
{
	public class HealthRecordManager : IHealthRecordManager
	{
		private readonly IHealthRecordRepo _healthRecordRepo;
		private readonly IGenericRepository<Encounter> _encounterRepo;
		private readonly IGenericRepository<Observation> _observationRepo;
		private readonly IGenericRepository<Condition> _conditionRepo;
		private readonly IUtilityManager _utiliyManager;
        public HealthRecordManager(IUtilityManager utilityManager,IHealthRecordRepo healthRecordRepo, IGenericRepository<Encounter> encounterRepo, IGenericRepository<Observation> observationRepo, IGenericRepository<Condition> conditionRepo)
        {
			_healthRecordRepo = healthRecordRepo;
			_encounterRepo = encounterRepo;
			_observationRepo = observationRepo;
			_conditionRepo = conditionRepo;
			_utiliyManager = utilityManager;
        }

		public async Task<HealthRecordsReadDto> GetAllHealthRecords(int patientId)
		{
			var context = (HealthMateContext)_observationRepo.GetContext();
			var encounters = _encounterRepo.GetAll().Where(p => p.PatientId == patientId).ToList();
			var conditions = (
				from condition in _conditionRepo.GetAll()
				join disease in context.Diseases on condition.DiseaseId equals disease.Disease_Id
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
			var observations = _observationRepo.GetAll().Where(p => p.PatientId == patientId).ToList();
			var patientIds = observations
				.Select(o => o.PatientId)
				.Distinct()
				.ToArray();
			var patients = context.Patients
				.Where(patient => patientIds.Contains(patient.Id))
				.ToDictionary(patient => patient.Id);
			var patientUserIds = observations
				.Select(o => patients.TryGetValue(o.PatientId, out var patient) ? patient.ApplicationUserId : null)
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct()
				.ToArray();
			var patientUsers = context.Users
				.Where(user => patientUserIds.Contains(user.Id))
				.ToDictionary(user => user.Id);
			var bodySiteIds = observations
				.Select(o => o.BodySiteId)
				.OfType<int>()
				.Distinct()
				.ToArray();
			var bodySites = context.BodySites
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
				Recorder = (Recorder)(int)x.Recorder,
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

			var healthRecordsDto = new HealthRecordsReadDto
			{
				Conditions = conditionList,
				Encounters = encounterList,
				Observations = observationList
			};

			return healthRecordsDto;
		}

		public async Task<GeneralPatientInformationReadDto> GetPatientGeneralInformation(int PatientId)
		{
			var rawData = await _healthRecordRepo.GetPatientGeneralInfoAsync(PatientId);

			var patientGeneralInformation = new GeneralPatientInformationReadDto { 
				Name = rawData.Name,
				Age = _utiliyManager.CalculateAgeReturnYearsOnly(rawData.BirthDate),
				Gender = rawData.Gender.ToString(),
				Height = rawData.Height.HasValue ? rawData.Height.Value.ToString() : "N/A",
				Weight = rawData.Weight.HasValue ? rawData.Weight.Value.ToString() : "N/A" 
			};

			return patientGeneralInformation;
		}

		public async Task<HealthRecordSummaryDto> GetAllHealthRecordSummary(int patientId){
			
			if (patientId <= 0)
			{
				throw new ArgumentException("Invalid patient ID", nameof(patientId));
			}

			// Execute all async operations in parallel
			var labTestsTask = _healthRecordRepo.getLabTestsSummary(patientId);
			var medicalImagesTask = _healthRecordRepo.getMedicalImagesSummary(patientId);
			var medicinesTask = _healthRecordRepo.getMedicineSummary(patientId);
			var conditionsTask = _healthRecordRepo.getConditionsSummary(patientId);
			var prescriptionsTask = _healthRecordRepo.getPrescriptionsSummary(patientId);
			var encountersTask = _healthRecordRepo.getEncounterSummary(patientId);

			// Wait for all tasks to complete
			await Task.WhenAll(
				labTestsTask, 
				medicalImagesTask, 
				medicinesTask, 
				conditionsTask, 
				prescriptionsTask,
				encountersTask
			);

			return new HealthRecordSummaryDto
			{
				LabTestsSummary = await labTestsTask,
				MedicalImagesSummary = await medicalImagesTask,
				MedicinesSummary = await medicinesTask,
				ConditionsSummary = await conditionsTask,
				PrescriptionsSummary = await prescriptionsTask,
				EncountersSummary = await encountersTask
			};
		}

		#region HealthRecords Details
		public async Task<PrescriptionDetailsReadDto> GetPrescriptionDetailsAsync(int PrescriptionId)
		{
			if (PrescriptionId <= 0)
				throw new ArgumentException("Invalid patient ID", nameof(PrescriptionId));

			return await _healthRecordRepo.getPrescriptionDetails(PrescriptionId);
		}

		public async Task<MedicalImageDetailsReadDto> GetMedicalImageDetailsAsync(int medicalImageId)
		{
			if (medicalImageId <= 0)
				throw new ArgumentException("Invalid medical image ID", nameof(medicalImageId));

			return await _healthRecordRepo.getMedicalImageDetails(medicalImageId);
		}

		public async Task<LabTestDetailsReadDto> GetLabTestDetailsAsync(int labTestId)
		{
			if (labTestId <= 0)
				throw new ArgumentException("Invalid lab test ID", nameof(labTestId));

			return await _healthRecordRepo.getLabTestDetails(labTestId);
		}

		public async Task<EncounterDetailsDto> GetEncounterDetailsAsync(int encounterId)
		{
			if (encounterId <= 0)
				throw new ArgumentException("Invalid encounter ID", nameof(encounterId));

			return await _healthRecordRepo.getEncounterDetails(encounterId);
		}
		#endregion
	}
}
