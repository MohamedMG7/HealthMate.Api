using HealthMate.Application.Observations.Contracts;

namespace HealthMate.Application.Manager.UtilityManager{
    public interface IUtilityManager{
        int CalculateAgeReturnYearsOnly(DateOnly birthDate);
        List<int> ExctractSystolicAndDiastolic(decimal bloodPressure);
        //Task<ObservationPatientDashboardReadDto> GetObservationRecordsAsync(int patientId, string observationType, int xDays, int yRecords);
    }
}