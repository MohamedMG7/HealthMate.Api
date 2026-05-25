using HealthMate.Infrastructure.DTO.ObservationDto;

namespace HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos{
    public class HumanPatientMobileDashboard{
        public HeartRateDto heartRate { get; set; } = null!;
        public bloodPressureDto bloodPressure {get;set;} = null!;
        public HemoglobinDto Hemoglobin { get; set; } = null!;
        public GlucoseLevelDto Glucose { get; set; } = null!;
        public string HighestBloodPressure { get; set; } = null!;
        public string LowestBloodPressure { get; set; } = null!;
    }
}