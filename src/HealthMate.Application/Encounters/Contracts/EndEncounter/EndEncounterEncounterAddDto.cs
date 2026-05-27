namespace HealthMate.Application.Encounters.Contracts{
	public class EndEncounterEncounterAddDto
	{
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public string Reason_To_Visit { get; set; } = null!;
		public string Treatment_Plan { get; set; } = null!;
		public string? Note { get; set; }
	}
}
