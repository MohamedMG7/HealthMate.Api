using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;

namespace HealthMate.Application.Manager.EncounterManager
{
	public interface IEncounterManager
	{
		[Obsolete("Use POST /api/Encounter/start; will be removed after Slice 5.")]
		void AddEncounter(EncounterAddDto encounter);
		IEnumerable<EncounterReadDto> GetAllEncounters();
		EncounterReadDto GetEncounter(int encounterId);
		void DeleteEncounter(int encounterId);
	}
}
