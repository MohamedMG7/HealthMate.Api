using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Condition;

namespace HealthMate.Application.Encounters.Contracts{
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
