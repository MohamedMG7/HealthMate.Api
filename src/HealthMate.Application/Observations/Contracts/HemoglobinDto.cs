namespace HealthMate.Application.Observations.Contracts{
    public class HemoglobinDto{
        public List<HemoglobinValueAndDateDto>? hemoglobinReadings { get; set; } 
        public bool IsUpdated { get; set; }
        public bool IsNormal { get; set; }
    }

    public class HemoglobinValueAndDateDto{
        public string Date { get; set; } = null!;
        public decimal HemoglobinValue { get; set; }
    }
}