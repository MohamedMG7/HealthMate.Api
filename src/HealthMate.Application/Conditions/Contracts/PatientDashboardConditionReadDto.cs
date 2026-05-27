namespace HealthMate.Application.Conditions.Contracts{
    public class PatientDashboardConditionReadDto{
        public string ConditionCode { get; set; } = null!;
        public string ConditionName { get; set; } = null!;
        public string ConditionDate { get; set; } = null!;
        public string Treatement { get; set; } = null!;
        public bool IsOngoing {get;set;}
    }
}