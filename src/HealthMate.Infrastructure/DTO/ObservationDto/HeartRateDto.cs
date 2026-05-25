namespace HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos{
    public class HeartRateDto{
        public int average { get; set; }
        public bool IsUpdated { get; set; }
        public bool IsSufficient { get; set; }
        public bool IsNormal { get; set; }
    }

    public class HeartRateValueAndDateDto{
        public string Date { get; set; } = null!;
        public decimal HeartRateValue { get; set; }
    }
}