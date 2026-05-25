using HealthMate.Infrastructure.DTO.HealthCareProviderDto;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.HealthCareProviderRepos;
using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Application.Manager.HealthRecordManager;
using HealthMate.Infrastructure.DTO.HealthRecordDto;
using Microsoft.EntityFrameworkCore;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos;
using HealthMate.Infrastructure.Repositories.MessageRepos;

namespace HealthMate.Application.Manager.HealthCareProviderManager
{
	public class HealthCareProviderManager : IHealthCareProviderManager
	{
        private readonly IHealthCareProviderRepo _HealthCareProviderRepo;
		private readonly IHealthRecordManager _HealthRecordManager;
		private readonly IPatientManager _patientManager;
		private readonly IMessageRepo _messageRepo;
		public HealthCareProviderManager(IMessageRepo messageRepo, IHealthCareProviderRepo HealthCareProviderRepo, IPatientManager patientManager,IHealthRecordManager HealthRecordManager)
        {
            _HealthCareProviderRepo = HealthCareProviderRepo;
			_patientManager = patientManager;
			_HealthRecordManager = HealthRecordManager;
			_messageRepo = messageRepo;
        }

		public async Task<string> GetHealthCareProviderNameById(int healthCareProviderId)
		{
			return await _HealthCareProviderRepo.GetHealthCareProviderNameById(healthCareProviderId);
		}

		public async Task<IEnumerable<EncounterTableSummaryReadDto>> GetRecentEncountersOrderedAsync(int healthCareProviderId)
		{
			var mappedData = await _HealthCareProviderRepo.GetRecentEncountersOrderedAsync(healthCareProviderId);
			return mappedData;
		}

		public async Task<int> GetTheCountOfPatientsDoctorEncounteredAsync(int healthCareProviderId)
		{
			return await _HealthCareProviderRepo.GetTheCountOfPatientsDoctorEncountered(healthCareProviderId);
		}

		private async Task<int> GetTotalCountOfUnreadMessages(string ApplicationUserId){
			return await _messageRepo.GetUnreadCountAsync(ApplicationUserId);
		}

		public async Task<int> GetTheCountOfTodayEncountersAsync(int healthCareProviderId)
		{
			return await _HealthCareProviderRepo.GetTheCountOfTodayEncounters(healthCareProviderId);
		}

		public async Task<int> GetTheCountOfTotalEncountersAsync(int healthCareProviderId)
		{
			return await _HealthCareProviderRepo.GetTheCountOfTotalEncounters(healthCareProviderId);
		}

		public async Task<bool> EndEncounter(EndEncounter endEcounterDto,int PatientId, int HealthcareProviderId){
			return await _HealthCareProviderRepo.EndEncounter(endEcounterDto,PatientId,HealthcareProviderId);
		}

		public async Task<patientDashboardDto> StartEncounter(string PatientNationalId)
		{
			try
			{
				// Get patient ID
				var patientId = await _HealthCareProviderRepo.GetPatientIdByPatientNationalId(PatientNationalId);
				
				// Get general information
				var patientGeneralData = await _HealthRecordManager.GetPatientGeneralInformation(patientId);
				
				// Get dashboard data (using default period of 30 days)
				var patientDashboardData = await _patientManager.GetPatientDashboardData(patientId, 30);

				

				// Combine all data into the DTO
				return new patientDashboardDto
				{
					patientId = patientId,
					patientImageUrl = patientDashboardData.patientImageUrl,
					heartRate = patientDashboardData.heartRate,
					PatientGeneralInformation = patientGeneralData,
					bloodPressure = patientDashboardData.bloodPressure,
					Hemoglobin = patientDashboardData.Hemoglobin,
					Glucose = patientDashboardData.Glucose,
					Condition = patientDashboardData.Condition,
					Documents = patientDashboardData.Documents,
					AppointmentHisory = patientDashboardData.AppointmentHisory,
					MentalHealthStatus = patientDashboardData.MentalHealthStatus
				};
			}
			catch (Exception ex)
			{
				// Log the error and rethrow
				Console.WriteLine($"Error in StartEncounter: {ex.Message}");
				throw;
			}
		}


		#region HealthCareProvider Dashboard
		public async Task<ClinicDashBoardDataDto> GetClinicalDashboardData(int healthCareProviderId)
		{
			// Validate input
			if (healthCareProviderId <= 0)
				throw new ArgumentException("Invalid healthcare provider ID", nameof(healthCareProviderId));

			var ApplicationUserId = await _HealthCareProviderRepo.GetApplicationUserId(healthCareProviderId);
			// Execute all async operations in parallel
			var totalEncountersTask = GetTheCountOfTotalEncountersAsync(healthCareProviderId);
			var todayEncountersTask = GetTheCountOfTodayEncountersAsync(healthCareProviderId);
			var totalPatientsTask = GetTheCountOfPatientsDoctorEncounteredAsync(healthCareProviderId);
			var totalOfUnreadMessages = GetTotalCountOfUnreadMessages(ApplicationUserId);
			var last7DaysTask = GetLast7DaysEncountersCountAsync(healthCareProviderId);
			var recentEncountersTask = GetRecentEncountersOrderedAsync(healthCareProviderId);
			var FrequentConditions = GetTop5FrequentConditionsNamesAsync(healthCareProviderId);
			var Specialization = GetHealthCareProviderSpecialization(healthCareProviderId);
			var HealthCareProviderName = GetHealthCareProviderNameById(healthCareProviderId);
			var HealthCareProviderimageUrl = GetHealthCareProviderImageUrl(healthCareProviderId);

			// Wait for all tasks to complete
			await Task.WhenAll(
				totalEncountersTask, 
				todayEncountersTask, 
				totalPatientsTask,
				totalOfUnreadMessages, 
				last7DaysTask, 
				recentEncountersTask,
				FrequentConditions,
				Specialization,
				HealthCareProviderName,
				HealthCareProviderimageUrl
			);

			return new ClinicDashBoardDataDto
			{
				TotalEncounters = await totalEncountersTask,
				TotalEncountersToday = await todayEncountersTask,
				TotalPatients = await totalPatientsTask,
				TotalOfUnreadMessages = await totalOfUnreadMessages,
				Last7DaysEncounters = await last7DaysTask,
				EncounterSummaray = await recentEncountersTask,
				FrequentConditions = await FrequentConditions,
				Specialization = await Specialization,
				name = await HealthCareProviderName,
				ImageUrl = await HealthCareProviderimageUrl
			};
		}

		public async Task<List<int>> GetLast7DaysEncountersCountAsync(int healthCareProviderId)
		{
			if (healthCareProviderId <= 0)
				throw new ArgumentException("Invalid healthcare provider ID", nameof(healthCareProviderId));

			return await _HealthCareProviderRepo.GetLast7DaysEncountersCountAsync(healthCareProviderId);
		}

		public async Task<List<ConditionFrequencyDto>> GetTop5FrequentConditionsNamesAsync(int healthCareProviderId){
			if(healthCareProviderId <= 0)
				throw new ArgumentException("Invalid healthcare provider ID", nameof(healthCareProviderId));

			return await _HealthCareProviderRepo.GetTop5FrequentConditionsWithCountAsync(healthCareProviderId);	
		}

		public async Task<string> GetHealthCareProviderSpecialization(int healthCareProviderId){
			if(healthCareProviderId <= 0)
				throw new ArgumentException("Invalid healthcare provider ID", nameof(healthCareProviderId));

			return await _HealthCareProviderRepo.GetHealthCareProviderSpecialization(healthCareProviderId);	
		}

		#endregion
		
		public async Task<string> GetHealthCareProviderImageUrl(int healthCareProviderId){
			if(healthCareProviderId <= 0)
				throw new ArgumentException("Invalid healthcare provider ID", nameof(healthCareProviderId));

			return await _HealthCareProviderRepo.GetHealthCareProviderImageUrl(healthCareProviderId);	
		}

	}
}
