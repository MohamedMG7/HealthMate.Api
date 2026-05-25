using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EncounterDto;

namespace HealthMate.Application.Manager.EncounterManager
{
	public interface IEncounterManager
	{
		void AddEncounter(EncounterAddDto encounter);
		IEnumerable<EncounterReadDto> GetAllEncounters();
		EncounterReadDto GetEncounter(int encounterId);
		void DeleteEncounter(int encounterId);
	}
}
