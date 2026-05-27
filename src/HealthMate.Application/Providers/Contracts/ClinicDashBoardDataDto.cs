using HealthMate.Application.Conditions.Contracts;

namespace HealthMate.Application.Providers.Contracts{
	public class ClinicDashBoardDataDto
	{
        public string name { get; set; } = null!;
        public string? Specialization {get; set;}
        public string ImageUrl { get; set; } = null!;
        public int TotalEncounters { get; set; }
		public int TotalEncountersToday { get; set; }
        public int TotalPatients { get; set; }
        public int TotalOfUnreadMessages { get; set; }
        public List<int>? Last7DaysEncounters {get; set;}
        public IEnumerable<EncounterTableSummaryReadDto> EncounterSummaray { get; set; } = null!;
        public List<ConditionFrequencyDto>? FrequentConditions { get; set; } 
    }
}
