using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.DTO.EndEcnounterDto
{
	public class EndEncounterConditionAddDto
	{
        public int DiseasesId { get; set; }
        public DateTime DateRecorded { get; set; }
        public ClinicalStatus ClinicalStatus { get; set; }
        public Recorder Recorder { get; set; }
        public Severity Severity { get; set; }
        public int? BodySite { get; set; }
        public string? Note { get; set; }

    }
}
