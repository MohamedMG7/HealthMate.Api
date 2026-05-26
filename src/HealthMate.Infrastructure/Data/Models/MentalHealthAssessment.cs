using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models
{
    /// <summary>
    /// Represents a patient's mental health assessment result (e.g., PHQ-9, GAD-7).
    /// </summary>
    public class MentalHealthAssessment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the patient who took the assessment.
        /// </summary>
        [Required]
        public int patientId { get; set; }

        /// <summary>
        /// Navigation property to the patient entity.
        /// </summary>
        public Patient Patient { get; set; } = null!;

        /// <summary>
        /// Type of assessment: e.g., PHQ9 or GAD7.
        /// </summary>
        [Required]
        public AssessmentType AssessmentType { get; set; }

        /// <summary>
        /// Encoded and encrypted answer string (e.g., "1242123").
        /// </summary>
        [Required]
        public string EncodedAnswers { get; set; } = null!;

        /// <summary>
        /// Calculated total score based on answers.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Timestamp of when the assessment was submitted.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
