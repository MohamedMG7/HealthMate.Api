using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;

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
