using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.Observations.Contracts{
	public class ObservationAddDto
	{
		public ObservationCategory Category { get; set; }
		public string? Code { get; set; }
		public string? CodeDisplayName { get; set; }
		public decimal? ValueQuanitity { get; set; }
		public string? ValueUnit { get; set; }
		public DateTime DateOfObservation { get; set; }
		public int PatientId { get; set; }
		public string? Interpertation { get; set; }
		public int? BodySiteId { get; set; }
	}
}
