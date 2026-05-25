using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.DTO.MentalHealthAssessmentDto
{
    /// <summary>
    /// DTO used when creating a new mental health assessment entry.
    /// </summary>
    public class CreateAssessmentDto
    {
        public AssessmentType AssessmentType { get; set; }
        public string? EncodedAnswers { get; set; } = null!;
        public int Score { get; set; } 
    }

    /// <summary>
    /// DTO for returning assessment result summaries.
    /// </summary>
    public class AssessmentResultDto
    {
        public AssessmentType AssessmentType { get; set; }
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO to retieve the last mental (mood, Anexity) Status for the patient
    /// </summary>
    public class MentalStatusDto
    {
        public string Mood { get; set; } = null!;
        public string Anxiety { get; set; } = null!;
    }
}
