using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Providers.Contracts;


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

		Task<int> GetPatientIdByPatientNationalId(string PatientNationalId);
		Task<List<int>> GetLast7DaysEncountersCountAsync(int healthCareProviderId);
		Task<List<ConditionFrequencyDto>> GetTop5FrequentConditionsWithCountAsync(int healthCareProviderId);
		Task<string> GetHealthCareProviderSpecialization(int healthCareProviderId);
		Task<string> GetHealthCareProviderImageUrl(int healthCareProviderId);
		
		Task<string> GetApplicationUserId(int healthCareProviderId);
	}
}
