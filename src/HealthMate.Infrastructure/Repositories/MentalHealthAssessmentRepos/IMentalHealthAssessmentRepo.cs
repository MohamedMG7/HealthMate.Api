using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// Interface for accessing mental health assessment data from the database.
    /// </summary>
    public interface IMentalHealthAssessmentRepo
    {
        Task AddAsync(MentalHealthAssessment assessment);
        Task<List<MentalHealthAssessment>> GetByPatientIdAsync(int patientId);
        Task<MentalHealthAssessment?> GetLatestPhq9Async(int patientId);
        Task<MentalHealthAssessment?> GetLatestGad7Async(int patientId);
    }
}
