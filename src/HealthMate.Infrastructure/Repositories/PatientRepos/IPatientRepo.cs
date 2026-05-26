using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO.EncounterDto;

namespace HealthMate.Infrastructure.Repositories.PatientRepos{
    [Obsolete("Use IPatientRepository; will be removed after the Patient aggregate migration completes.")]
    public interface IPatientRepo : IGenericRepository<Patient>{
        Task<string> GetPatientImageUrl(int patientId);
        Task<int> GetPatientAgeByPatientId(int patientId);
        Task<string> GetPatientGenderByPatientId(int patientId);
        Task<List<patientDashboardEncounterHistory>> Get4RecentEncounters(int patientId);
        Task<DateOnly> GetPatientAge(int patientId);

        
    }
}
