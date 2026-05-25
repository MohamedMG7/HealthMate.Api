using HealthMate.Infrastructure.Enums;

namespace EndEncounterDto{
    public class EndEncounterObservationAddDto{
        public ObservationCategory Category { get; set; }
		public string? Code { get; set; }
		public string ObservationName { get; set; } = null!;
		public decimal ValueQuanitity { get; set; }
		public string? ValueUnit { get; set; }
		public string? Interpertation { get; set; }
		public int? BodySiteId { get; set; }
    }
}