using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.DTO.ConditionDto
{
	public class ConditionAddDto
	{
        public int Patient_Id { get; set; }
        public int DiseasesId { get; set; }
        public int? Encounter_Id { get; set; }
        public DateTime DateRecorded { get; set; }
        public ClinicalStatus ClinicalStatus { get; set; }
        public Recorder Recorder { get; set; }
        public Severity Severity { get; set; }
        public int? BodySite { get; set; }
        public string? Note { get; set; }

    }
}
