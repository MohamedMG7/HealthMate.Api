using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.ObservationDto;

namespace HealthMate.Infrastructure.DTO.HealthRecordDto
{
	public class HealthRecordsReadDto
	{
        public IEnumerable<ConditionReadDto> Conditions { get; set; }
        public IEnumerable<EncounterReadDto> Encounters { get; set; }
        public IEnumerable<ObservationReadDto> Observations { get; set; }
    }
}
