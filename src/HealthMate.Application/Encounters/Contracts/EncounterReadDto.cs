namespace HealthMate.Application.Encounters.Contracts{
	public class EncounterReadDto
	{
        public int Encounter_Id { get; set; }
        public string Encounter_Fhir_Id { get; set; } = null!;
        public int Patient_Id { get; set; }
		public int HealthcareProvider_Id { get; set; }
		public DateTime StartDate { get; set; } // record when Encounter start
		public DateTime EndDate { get; set; } // record when encounter end
		public string? Location { get; set; }
		public string Reason_To_Visit { get; set; } = null!;
		public string Treatment_Plan { get; set; } = null!;
		public string? Note { get; set; }
        public bool isDeleted { get; set; }
    }
}
