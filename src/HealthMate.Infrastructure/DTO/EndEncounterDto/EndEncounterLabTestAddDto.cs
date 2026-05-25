using HealthMate.Infrastructure.DTO.LabTestDto;

namespace EndEncounterDto{
    public class EndEncounterLabTestAddDto{
        public string LabTestName { get; set; } = null!;
        public DateTime RecordedTime { get; set; }
        public List<LabTestResultDto> Results { get; set; } = new List<LabTestResultDto>();
        public string? Note { get; set; }
    }
}