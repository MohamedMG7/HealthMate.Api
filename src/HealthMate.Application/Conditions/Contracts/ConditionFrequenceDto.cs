namespace HealthMate.Application.Conditions.Contracts{
    public class ConditionFrequencyDto
    {
        public string ConditionName { get; set; } = null!;
        public int Frequency { get; set; }
    }
}
