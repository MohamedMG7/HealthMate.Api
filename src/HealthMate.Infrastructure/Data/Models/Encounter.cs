namespace HealthMate.Infrastructure.Data.Models
{
	public class Encounter
	{
        public int Encounter_Id { get; set; }
		public string Encounter_Fhir_Id { get; set; } = null!;

        // Link With Patient
        public Patient Patient { get; set; } = null!;
        public int PatientId { get; set; }

        //Link With HealthCare Provider
        public HealthCareProvider HealthCareProvider { get; set; } = null!;
        public int HealthCareProviderId { get; set; }
        

        public DateTime StartDate { get; set; } // record when Encounter start
        public DateTime EndDate { get; set; } // record when encounter end
        public string? Location { get; set; }
        public string Reason_To_Visit { get; set; } = null!;
        public string Treatment_Plan { get; set; } = null!;
        public string? Note { get; set; }

        public bool isDeleted { get; set; }

        //Link with conditions one to many an Encounter can have multiple conditions
        public ICollection<Condition> Conditions { get; set; } = new HashSet<Condition>();
        public ICollection<Prescription> Prescriptions { get; set; } = new HashSet<Prescription>();

    }
}
