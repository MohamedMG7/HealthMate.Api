using HealthMate.Application.MentalHealth.Contracts;

namespace HealthMate.Application.Managers
{
    /// <summary>
    /// Interface for handling mental health assessments (PHQ-9, GAD-7).
    /// </summary>
    public interface IMentalHealthAssessmentManager
    {
        /// <summary>
        /// Stores a new mental health assessment result.
        /// </summary>
        Task SubmitAssessmentAsync(int patientId, CreateAssessmentDto dto);

        /// <summary>
        /// Gets the latest mood (PHQ-9) and anxiety (GAD-7) statuses for a patient.
        /// </summary>
        Task<MentalStatusDto> GetLatestMentalStatusAsync(int patientId);
    }
}
