using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EndEcnounterDto;
using HealthMate.Infrastructure.DTO.HealthCareProviderDto;


namespace HealthMate.Infrastructure.Repositories.HealthCareProviderRepos
{
	public interface IHealthCareProviderRepo : IGenericRepository<HealthCareProvider>
	{
		Task<int> GetTheCountOfTodayEncounters(int healthCareProviderId);
		Task<int> GetTheCountOfPatientsDoctorEncountered(int healthCareProviderId);
		//int GetTheCountOfAllUnreadMessagesFromPatients(); not now
		Task<int> GetTheCountOfTotalEncounters(int healthCareProviderId);
		//IEnumerable<EncounterTableSummaryReadDto> GetRecentEncountersOrdered();
		Task<string> GetHealthCareProviderNameById(int healthCareProviderId);

		Task<IEnumerable<EncounterTableSummaryReadDto>> GetRecentEncountersOrderedAsync(int healthCareProviderId);

		Task<int> AddEncounterAndReturnEncounterId(EndEncounterEncounterAddDto encounterData, int PatientId, int HealthCareProvider);
		Task<bool> EndEncounter(EndEncounter endEcounterDto,int PatientId, int HealthcareProviderId);
		Task<int> GetPatientIdByPatientNationalId(string PatientNationalId); // this should return the patient Id if existed
		Task<List<int>> GetLast7DaysEncountersCountAsync(int healthCareProviderId);
		Task<List<ConditionFrequencyDto>> GetTop5FrequentConditionsWithCountAsync(int healthCareProviderId);
		Task<string> GetHealthCareProviderSpecialization(int healthCareProviderId);
		Task<string> GetHealthCareProviderImageUrl(int healthCareProviderId);
		
		Task<string> GetApplicationUserId(int healthCareProviderId);
	}
}
