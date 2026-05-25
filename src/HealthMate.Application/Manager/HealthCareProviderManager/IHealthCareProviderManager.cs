using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.HealthCareProviderDto;
using HealthMate.Infrastructure.DTO.HealthRecordDto;
using HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos;

namespace HealthMate.Application.Manager.HealthCareProviderManager
{
	public interface IHealthCareProviderManager
	{
		Task<int> GetTheCountOfTodayEncountersAsync(int healthCareProviderId);
		Task<int> GetTheCountOfPatientsDoctorEncounteredAsync(int healthCareProviderId);
		//int GetTheCountOfAllUnreadMessagesFromPatients(); not now
		Task<int> GetTheCountOfTotalEncountersAsync(int healthCareProviderId);
		Task<IEnumerable<EncounterTableSummaryReadDto>> GetRecentEncountersOrderedAsync(int healthCareProviderId);
		Task<string> GetHealthCareProviderNameById(int healthCareProviderId);

		Task<bool> EndEncounter(EndEncounter endEcounterDto,int PatientId, int HealthcareProviderId);
		Task<patientDashboardDto> StartEncounter(string PatientNationalId);
		Task<ClinicDashBoardDataDto> GetClinicalDashboardData(int HealthCareProviderId);
		Task<List<int>> GetLast7DaysEncountersCountAsync(int healthCareProviderId);
		
	}
}
