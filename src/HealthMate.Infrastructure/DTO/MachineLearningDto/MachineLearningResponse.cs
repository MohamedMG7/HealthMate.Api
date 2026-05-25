namespace HealthMate.Infrastructure.DTO.MachineLearningDto{
    public class MachineLearningResponse{
        public bool Animea { get; set; }
        public bool Diabetes { get; set;} = false;
        public bool Hypertenstion { get; set; } = false;
        public bool ChronicKidneyDisease { get; set; } = false;
        public bool HeartDisease { get; set; } = false;
    }
}