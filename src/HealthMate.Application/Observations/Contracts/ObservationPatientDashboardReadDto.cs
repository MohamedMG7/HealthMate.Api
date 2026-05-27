namespace HealthMate.Application.Observations.Contracts{
    public class ObservationPatientDashboardReadDto
    {
        public List<ObservationValueAndDateRead>? Readings { get; set; } 
        public bool IsUpdated { get; set; } 
    }
}