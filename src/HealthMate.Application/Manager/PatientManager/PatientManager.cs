using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Identity;
using HealthMate.Infrastructure.DTO.PatientDto.AnimalPatientDtos;
using HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.PatientRepos;
using HealthMate.Application.Manager.UtilityManager;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using HealthMate.Application.Manager.ObservationManager;
using HealthMate.Application.Manager.ConditionManager;
using HealthMate.Application.Managers;

namespace HealthMate.Application.Manager.PatientManager
{
    public class PatientManager : IPatientManager
	{
		private readonly IPatientRepo _patientRepo;
		private readonly IGenericRepository<Animal> _animalRepo;
		private readonly IObservationManager _observationManager;
		private readonly IObservationRepo _observationRepo;
		private readonly IUtilityManager _utilityManager;
		private readonly IConditionManager _conditionManager;
		private readonly IMentalHealthAssessmentManager _mentalHealthAssessmentManager;
        public PatientManager(IMentalHealthAssessmentManager mentalHealthAssessmentManager, IObservationManager observationManager,IObservationRepo observationRepo,IUtilityManager utilityManager, IPatientRepo patientRepo, IGenericRepository<Animal> animalRepo, IConditionManager conditionManager)
        {
            _patientRepo = patientRepo;
			_animalRepo = animalRepo;
			_utilityManager = utilityManager;
			_observationRepo = observationRepo;
			_observationManager = observationManager;
			_conditionManager = conditionManager;
			_mentalHealthAssessmentManager = mentalHealthAssessmentManager;
        }

		public void AddAnimalPatient(AnimalPatientAddDto AnimalPatient)
		{
			var APatient = new Animal
			{
				Name = AnimalPatient.Name,
				BirthDate = AnimalPatient.BirthDate,
				Gender = AnimalPatient.Gender,
				Species = AnimalPatient.Species!,
				Breed = AnimalPatient.Breed!,
				Owner_Id = AnimalPatient.Owner_Id 
			};

			_animalRepo.Add(APatient);
			_animalRepo.Save();
		}

		public void AddHumanPatient(HumanPatientAddDto HumanPatient)
		{
			var HPatient = Patient.Create(
				NationalId.Create(HumanPatient.NationalId),
				HumanPatient.BirthDate,
				HumanPatient.Gender,
				Governorate.Create(HumanPatient.Governorate),
				City.Create(HumanPatient.City),
				UserId.Create(HumanPatient.ApplicationUserId),
				HumanPatient.NationalIdImageUrl,
				HumanPatient.Weight,
				HumanPatient.Height);

			_patientRepo.Add(HPatient);
			_patientRepo.Save();
		}

		public IEnumerable<HumanPatientReadDto> GetAllHumanPatients()
		{
			var Humanpatients = _patientRepo.GetAll().ToList();

			var HumanPatientsList = Humanpatients.Select(x => new HumanPatientReadDto { 
				Patient_Id = x.Patient_Id,
				Patient_Fhir_Id = x.Patient_Fhir_Id,
				BirthDate = x.BirthDate,
				City = x.City.Value,
				Gender = x.Gender,
				Governorate = x.Governorate.Value,
				IsVerified = x.IsVerified,
				NationalId = x.NationalId.Value,
				NationalIdImageUrl = x.NationalIdImageUrl,
				Weight = x.Weight,
				Height = x.Height
			});

			return HumanPatientsList;
		}

		public IEnumerable<VerifiedHumanPatientReadDto> GetAllVerifiedHumanPatients()
		{
			var VerifiedHumanpatients = _patientRepo.GetAll().Where(x => x.IsVerified == true).ToList();

			var VerifiedHumanPatientsList = VerifiedHumanpatients.Select(x => new VerifiedHumanPatientReadDto
			{
				Patient_Id = x.Patient_Id,
				Patient_Fhir_Id = x.Patient_Fhir_Id,
				BirthDate = x.BirthDate,
				City = x.City.Value,
				Gender = x.Gender,
				Governorate = x.Governorate.Value,
				NationalId = x.NationalId.Value,
				NationalIdImageUrl = x.NationalIdImageUrl,
				Weight = x.Weight,
				Height = x.Height
			});

			return VerifiedHumanPatientsList;
		}

		public async Task<patientDashboardDto> GetPatientDashboardData(int patientId, int periodInDays)
		{
			var heartRateTask = _observationManager.GetAverageHeartrateInXTime(patientId, periodInDays); 
			var imageUrlTask = _patientRepo.GetPatientImageUrl(patientId);
			var bloodPressureTask = _observationManager.GetAverageBloodPressureInXTime(patientId, periodInDays);
			var hemoglobinTask = _observationManager.GetHemoglobinDataInXTime(patientId, periodInDays);
			var glucoseTask = _observationManager.GetAverageGlucoseInXTime(patientId, periodInDays);
			var mostRecentSevereOngoingConditionTask = _conditionManager.getMostRecentSevereOngoingCondition(patientId);
			var mostRecentDocumentsTask = _observationRepo.GetMostRecentDocuments(patientId);
			var AppointmentHisoryTask = _patientRepo.Get4RecentEncounters(patientId);
			
			var mentalHealthTask = _mentalHealthAssessmentManager.GetLatestMentalStatusAsync(patientId);

			// Wait for all tasks to complete
			await Task.WhenAll(mentalHealthTask, AppointmentHisoryTask,heartRateTask, imageUrlTask, bloodPressureTask, hemoglobinTask, glucoseTask, mostRecentSevereOngoingConditionTask, mostRecentDocumentsTask);

			return new patientDashboardDto
			{
				heartRate = await heartRateTask,
				patientImageUrl = await imageUrlTask,
				bloodPressure = await bloodPressureTask,
				Hemoglobin = await hemoglobinTask,
				Glucose = await glucoseTask,
				Condition = await mostRecentSevereOngoingConditionTask,
				Documents = await mostRecentDocumentsTask,
				AppointmentHisory = await AppointmentHisoryTask,
				MentalHealthStatus = await mentalHealthTask
			};
		}

		public async Task<HumanPatientMobileDashboard> GetMobilePatientDashboardDataAsync(int patientId)
		{
			int defaultPeriodInDays = 30;

			var heartRateTask = _observationManager.GetAverageHeartrateInXTime(patientId, defaultPeriodInDays);
			var bloodPressureTask = _observationManager.GetAverageBloodPressureInXTime(patientId, defaultPeriodInDays);
			var hemoglobinTask = _observationManager.GetHemoglobinDataInXTime(patientId, defaultPeriodInDays);
			var glucoseTask = _observationManager.GetAverageGlucoseInXTime(patientId, defaultPeriodInDays);
			var highestBloodPressureTask = _observationRepo.GetHighestBloodPressureAsync(patientId, defaultPeriodInDays);
			var lowestBloodPressureTask = _observationRepo.GetLowestBloodPressureAsync(patientId, defaultPeriodInDays);

			await Task.WhenAll(heartRateTask, bloodPressureTask, hemoglobinTask, glucoseTask, highestBloodPressureTask, lowestBloodPressureTask);

			var highestBloodPressure = await highestBloodPressureTask;
			var lowestBloodPressure = await lowestBloodPressureTask;

			List<int> highestValues;
			List<int> lowestValues;

			if (decimal.TryParse(highestBloodPressure, out var highestVal))
			{
				highestValues = _utilityManager.ExctractSystolicAndDiastolic(highestVal);
			}
			else
			{
				highestValues = new List<int> { 0, 0 };
			}

			if (decimal.TryParse(lowestBloodPressure, out var lowestVal))
			{
				lowestValues = _utilityManager.ExctractSystolicAndDiastolic(lowestVal);
			}
			else
			{
				lowestValues = new List<int> { 0, 0 };
			}

			return new HumanPatientMobileDashboard
			{
				heartRate = await heartRateTask,
				bloodPressure = await bloodPressureTask,
				Hemoglobin = await hemoglobinTask,
				Glucose = await glucoseTask,
				HighestBloodPressure = $"{highestValues[0]}/{highestValues[1]}",
				LowestBloodPressure = $"{lowestValues[0]}/{lowestValues[1]}"
			};
		}



		

	}
}
