namespace HealthMate.Infrastructure.Repositories
{
    public interface IValidationRepo
    {
        Task<bool> CheckPatientId(int PatientId);
        Task<bool> CheckPatientNationalId(string PatientNationalId);
        Task<bool> CheckHealthcareProviderId(int HealthCareProviderId);
    }
}