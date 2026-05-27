namespace HealthMate.Application.Conditions.Contracts{
    public class ConditionSummaryReadDto{
        public int ConditionId { get; set; }
        public string ConditionName { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string Severity { get; set; } = null!;
        public string Note { get; set; } = null!;
    }
}