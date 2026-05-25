using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models
{
	public class Patient
	{
        
        public int Patient_Id { get; set; } // internal - incremental
		public string Patient_Fhir_Id { get; set; } = null!;//GUID
        public string NationalId { get; set; } = null!;
        public string NationalIdImageUrl { get; set; } = null!;
        public DateOnly BirthDate { get; set; }
        public Gender Gender { get; set; }
		public string Governorate { get; set; } = null!;
		public string City { get; set; } = null!;
        public bool IsVerified { get; set; }

        //link with application user to handle account
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public string? ApplicationUserId { get; set; }

		//Patient General Data
		public float? Weight { get; set; } //changes alot
		public float? Height { get; set; }

		// Link To Address
		//public Address Address { get; set; }

		public ICollection<Condition> Conditions { get; set; } = new HashSet<Condition>();
		public ICollection<Encounter> Encounters { get; set; } = new HashSet<Encounter>();
		public ICollection<Observation> Observations { get; set; } = new HashSet<Observation>();
        public ICollection<Animal> Animals { get; set; } = new HashSet<Animal>();
		public ICollection<LabTest> LabTests { get; set; } = new HashSet<LabTest>();
		public ICollection<PatientMedicine> PatientMedicines { get; set; } = new HashSet<PatientMedicine>();
		public ICollection<MedicalImage> MedicalImages { get; set; } = new HashSet<MedicalImage>();
		public ICollection<MentalHealthAssessment> MentalHealthAssessments { get; set; } = new HashSet<MentalHealthAssessment>();


    }

    
}
