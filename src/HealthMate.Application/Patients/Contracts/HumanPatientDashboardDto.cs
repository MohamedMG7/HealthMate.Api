using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.HealthRecord.Contracts;
using HealthMate.Application.MentalHealth.Contracts;
using HealthMate.Application.Observations.Contracts;

namespace HealthMate.Application.Patients.Contracts{
    public class patientDashboardDto{
        public int patientId { get; set; }
        public string patientImageUrl { get; set; } = null!;
        public HeartRateDto heartRate { get; set; } = null!;
        public GeneralPatientInformationReadDto PatientGeneralInformation { get; set; } = null!;
        public bloodPressureDto bloodPressure {get;set;} = null!;
        public HemoglobinDto Hemoglobin { get; set; } = null!;
        public GlucoseLevelDto Glucose { get; set; } = null!;
        public PatientDashboardConditionReadDto Condition { get; set; } = null!;
        public List<DocumentDto> Documents { get; set; } = null!;
        public List<patientDashboardEncounterHistory> AppointmentHisory { get; set; } = null!;
        public MentalStatusDto MentalHealthStatus { get; set; } = null!;
    
    }
}