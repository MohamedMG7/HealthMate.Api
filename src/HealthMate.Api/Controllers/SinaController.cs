using HealthMate.Application.Manager.SinaChatbot;
using HealthMate.Infrastructure.DTO.SinaDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers{
    //[Authorize(policy:"HealthCareProviderOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class SinaController : ControllerBase{
        private readonly GeminiClient _geminiClient;
        private readonly SinaManager _sinaManager;

        public SinaController(GeminiClient geminiClient, SinaManager sinaManager)
        {
            _geminiClient = geminiClient;
            _sinaManager = sinaManager;
        }

        /// <summary>
        /// Basic Gemini response without MCP enrichment.
        /// </summary>
        [HttpPost("ask")]
        public async Task<IActionResult> AskSina([FromBody] GeminiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
                return BadRequest("Prompt is required.");

            var reply = await _geminiClient.AskAsync(request.Prompt);
            return Ok(new { reply });
        }

        /// <summary>
        /// Gemini response with MCP context enrichment (resolves @References).
        /// </summary>
        [HttpPost("ask-with-mcp")]
        public async Task<IActionResult> AskSinaWithMcp([FromBody] GeminiRequest request, [FromQuery] int patientId)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
                return BadRequest("Prompt is required.");

            if (patientId <= 0)
                return BadRequest("Valid patientId is required.");

            var reply = await _sinaManager.HandlePromptAsync(request.Prompt, patientId);
            return Ok(new { reply });
        }

        [HttpGet("references/{patientId}")]
        public async Task<IActionResult> GetReferences(int patientId)
        {
            var references = await _sinaManager.GetMCPReferencesAsync(patientId);
            return Ok(references);
        }
    }
}