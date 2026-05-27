using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Application.Manager.UtilityManager;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using HealthMate.Application.Manager.ObservationManager;
using HealthMate.Application.Manager.ConditionManager;
using HealthMate.Application.Managers;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Application.Manager.PatientManager
{
    public class PatientManager : IPatientManager
	{
		private readonly IGenericRepository<Animal> _animalRepo;
		private readonly IObservationManager _observationManager;
		private readonly IObservationRepo _observationRepo;
		private readonly IUtilityManager _utilityManager;
		private readonly IConditionManager _conditionManager;
		private readonly IMentalHealthAssessmentManager _mentalHealthAssessmentManager;
        private readonly IDbContextFactory<HealthMateContext> _contextFactory;

        public PatientManager(IMentalHealthAssessmentManager mentalHealthAssessmentManager, IObservationManager observationManager,IObservationRepo observationRepo,IUtilityManager utilityManager, IGenericRepository<Animal> animalRepo, IConditionManager conditionManager, IDbContextFactory<HealthMateContext> contextFactory)
        {
			_animalRepo = animalRepo;
			_utilityManager = utilityManager;
			_observationRepo = observationRepo;
			_observationManager = observationManager;
			_conditionManager = conditionManager;
			_mentalHealthAssessmentManager = mentalHealthAssessmentManager;
            _contextFactory = contextFactory;
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

		public async Task<patientDashboardDto> GetPatientDashboardData(int patientId, int periodInDays)
		{
			var heartRateTask = _observationManager.GetAverageHeartrateInXTime(patientId, periodInDays); 
			var imageUrlTask = GetPatientImageUrl(patientId);
			var bloodPressureTask = _observationManager.GetAverageBloodPressureInXTime(patientId, periodInDays);
			var hemoglobinTask = _observationManager.GetHemoglobinDataInXTime(patientId, periodInDays);
			var glucoseTask = _observationManager.GetAverageGlucoseInXTime(patientId, periodInDays);
			var mostRecentSevereOngoingConditionTask = _conditionManager.getMostRecentSevereOngoingCondition(patientId);
			var mostRecentDocumentsTask = _observationRepo.GetMostRecentDocuments(patientId);
			var AppointmentHisoryTask = Get4RecentEncounters(patientId);
			
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

        private async Task<string> GetPatientImageUrl(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var applicationUserId = await context.Patients
                .Where(patient => patient.Id == patientId)
                .Select(patient => patient.ApplicationUserId)
                .FirstOrDefaultAsync();

            if (applicationUserId is null)
            {
                throw new ArgumentNullException(nameof(patientId), "Patient Image Url Not Found");
            }

            var patientImageUrl = await context.Users
                .Where(user => user.Id == applicationUserId)
                .Select(user => user.ImageUrl)
                .FirstOrDefaultAsync();

            if (patientImageUrl is null)
            {
                throw new ArgumentNullException(nameof(patientId), "Patient Image Url Not Found");
            }

            return patientImageUrl;
        }

        private async Task<List<patientDashboardEncounterHistory>> Get4RecentEncounters(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var recentEncounters = await context.Encounters
                .Where(encounter => encounter.PatientId == patientId)
                .OrderByDescending(encounter => encounter.EndDate)
                .Take(4)
                .Select(encounter => new
                {
                    encounter.Encounter_Id,
                    HealthCareProviderFirstName = encounter.HealthCareProvider.ApplicationUser.First_Name,
                    HealthCareProviderLastName = encounter.HealthCareProvider.ApplicationUser.Last_Name,
                    encounter.HealthCareProvider.Specialization,
                    encounter.StartDate
                })
                .ToListAsync();

            return recentEncounters.Select(encounter => new patientDashboardEncounterHistory
                {
                    EncounterId = encounter.Encounter_Id,
                    HcpName = "DR/ " + encounter.HealthCareProviderFirstName + " " + encounter.HealthCareProviderLastName,
                    HcpSpecialization = encounter.Specialization,
                    EncounterDate = encounter.StartDate.ToString("yyyy-MM-dd")
                })
                .ToList();
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
