using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Infrastructure.Data.Models
{
	public class Observation
	{
        public int Observation_Id { get; set; }
		public string Observation_Fhir_Id { get; set; } = null!;// GUID
        public ObservationCategory Category { get; set; }
        public string? Code { get; set; }
        public string? CodeDisplayName { get; set; }
        public decimal? ValueQuanitity { get; set; }
        public string? ValueUnit { get; set; }
        public DateTime DateOfObservation { get; set; }
        
        //Link With Patient
        public Patient Patient { get; set; } = null!;
        public int PatientId { get; set; }

        public string? Interpertation { get; set; }

        //Link With BodySite
        public BodySite BodySite { get; set; } = null!;
        public int? BodySiteId { get; set; }

        public string NameIdentifier { get; set; } = null!; // this is used as identifier to sina

        public bool isDeleted { get; set; }
    }

    
}
