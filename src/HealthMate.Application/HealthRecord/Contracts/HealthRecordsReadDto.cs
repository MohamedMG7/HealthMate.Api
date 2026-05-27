using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Observations.Contracts;

namespace HealthMate.Application.HealthRecord.Contracts{
	public class HealthRecordsReadDto
	{
        public IEnumerable<ConditionReadDto> Conditions { get; set; }
        public IEnumerable<EncounterReadDto> Encounters { get; set; }
        public IEnumerable<ObservationReadDto> Observations { get; set; }
    }
}
