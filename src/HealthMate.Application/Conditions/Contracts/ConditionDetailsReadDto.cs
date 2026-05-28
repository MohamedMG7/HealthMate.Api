namespace HealthMate.Application.Conditions.Contracts{
    public class ConditionDetailsReadDto{
        public string DiseaseName { get; set; } = null!;
        public int PaientId { get; set; }
		public string DateRecorded { get; set; } = null!;
		public string ClinicalStatus { get; set; } = null!;
		public string Recorder { get; set; } = null!;
		public string Severity { get; set; } = null!;
		public string? Note { get; set; }
    }
}
