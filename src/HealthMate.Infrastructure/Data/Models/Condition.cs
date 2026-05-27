using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Infrastructure.Data.Models
{
	public class Condition
	{
        public int Condition_Id { get; set; }
		public string Condition_Fhir_Id { get; set; } = null!;
        
        //Link With Patient
        public Patient Patient { get; set; } = null!;
        public int PaientId { get; set; }

		//Link With Encounter should be nullable
		public Encounter Encounter { get; set; } = null!;
		public int? EncounterId { get; set; }

		public DateTime DateRecorded { get; set; }
        public ClinicalStatus ClinicalStatus { get; set; }
        public Recorder Recorder { get; set; }

        public Disease Disease { get; set; } = null!;
        public int Disease_Id { get; set; }
        public Severity Severity { get; set; }

        //Link With BodySite
        public BodySite BodySite { get; set; } = null!;
        public int? BodySiteId { get; set; }

        //public Stage Stage { get; set; }

        public string? Note { get; set; }

        public bool isOngoing { get; set; }

        public bool isChronic { get; set; } //healthcare provider should determine if this is chronic or not / what is chronic (documentation)

        public UserType AddedBy { get; set; } // did the patient add this medical record or the healthcare provider ?

    }

	

   
}
