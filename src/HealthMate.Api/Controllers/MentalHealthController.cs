using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HealthMate.Infrastructure.DTO.MentalHealthAssessmentDto;
using HealthMate.Application.Managers;

namespace HealthMate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "PatientOnly")]
    public class MentalHealthAssessmentController : ControllerBase
    {
        private readonly IMentalHealthAssessmentManager _manager;

        public MentalHealthAssessmentController(IMentalHealthAssessmentManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Submit a new mental health assessment (PHQ-9 or GAD-7).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitAssessment([FromBody] CreateAssessmentDto dto)
        {
            if (!int.TryParse(User.FindFirst("PatientId")?.Value, out int patientId))
                return Unauthorized("Invalid or missing PatientId claim.");

            await _manager.SubmitAssessmentAsync(patientId, dto);
            return Ok("Assessment submitted successfully.");
        }

        /// <summary>
        /// Get the latest PHQ-9 and GAD-7 results for the authenticated patient.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetMentalStatus()
        {
            if (!int.TryParse(User.FindFirst("PatientId")?.Value, out int userId))
                return Unauthorized("Invalid user ID in token.");

            var result = await _manager.GetLatestMentalStatusAsync(userId);
            return Ok(result);
        }
    }
}
