using HealthMate.Application.MentalHealth.Contracts;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories.Interfaces;

namespace HealthMate.Application.Managers
{
    /// <summary>
    /// Manages logic for submitting and analyzing mental health assessments.
    /// </summary>
    public class MentalHealthAssessmentManager : IMentalHealthAssessmentManager
    {
        private readonly IMentalHealthAssessmentRepo _repo;

        public MentalHealthAssessmentManager(IMentalHealthAssessmentRepo repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Submits a new PHQ-9 or GAD-7 assessment using a pre-calculated score.
        /// </summary>
        public async Task SubmitAssessmentAsync(int patientId, CreateAssessmentDto dto)
        {
            var assessment = new MentalHealthAssessment
            {
                patientId = patientId,
                AssessmentType = dto.AssessmentType,
                EncodedAnswers = dto.EncodedAnswers ?? "No Answers Saved",
                Score = dto.Score
            };

            await _repo.AddAsync(assessment);
        }

        /// <summary>
        /// Gets the patient's most recent mood and anxiety assessment statuses.
        /// </summary>
        public async Task<MentalStatusDto> GetLatestMentalStatusAsync(int patientId)
        {
            var latestMood = await _repo.GetLatestPhq9Async(patientId);
            var latestAnxiety = await _repo.GetLatestGad7Async(patientId);

            string moodStatus = "No recent mood test";
            string anxietyStatus = "No recent anxiety test";

            if (latestMood != null)
            {
                moodStatus = latestMood.Score switch
                {
                    <= 4 => "Minimal depression",
                    <= 9 => "Mild depression",
                    <= 14 => "Moderate depression",
                    <= 19 => "Moderately severe depression",
                    _ => "Severe depression"
                };
            }

            if (latestAnxiety != null)
            {
                anxietyStatus = latestAnxiety.Score switch
                {
                    <= 4 => "Minimal anxiety",
                    <= 9 => "Mild anxiety",
                    <= 14 => "Moderate anxiety",
                    _ => "Severe anxiety"
                };
            }

            return new MentalStatusDto
            {
                Mood = moodStatus,
                Anxiety = anxietyStatus
            };
        }
    }
}
