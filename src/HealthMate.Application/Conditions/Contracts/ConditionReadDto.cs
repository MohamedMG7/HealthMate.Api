using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Condition;

namespace HealthMate.Application.Conditions.Contracts{
	public class ConditionReadDto
	{
		public int Condition_Id { get; set; }
		public string Condition_Fhir_Id { get; set; } = null!;
        public string DiseaseName { get; set; } = null!;
        public int PaientId { get; set; }
		public int? EncounterId { get; set; }
		public DateTime DateRecorded { get; set; }
		public ClinicalStatus ClinicalStatus { get; set; }
		public Recorder Recorder { get; set; }
		public Severity Severity { get; set; }
		public int? BodySiteId { get; set; }
		public string? Note { get; set; }
	}
}
