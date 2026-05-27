using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.Observations.Contracts{
	public class ObservationReadDto
	{
		public int Observation_Id { get; set; }
		public string Observation_Fhir_Id { get; set; } 
		public ObservationCategory Category { get; set; }
		public string? Code { get; set; }
		public string? CodeDisplayName { get; set; }
		public decimal? ValueQuanitity { get; set; }
		public string? ValueUnit { get; set; }
		public DateTime DateOfObservation { get; set; }
		public string PatientName { get; set; } 
		public string? Interpertation { get; set; }
		public string? BodySiteName { get; set; }
	}
}
