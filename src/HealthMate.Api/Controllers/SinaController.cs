using HealthMate.Sina.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers;

[Authorize(Policy = "HealthCareProviderOnly")]
[Route("api/[controller]")]
[ApiController]
public class SinaController : ControllerBase
{
    private readonly SinaManager sinaManager;

    public SinaController(SinaManager sinaManager)
    {
        this.sinaManager = sinaManager;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<OpenSessionResponse>> OpenSession([FromBody] OpenSinaSessionRequest request, CancellationToken ct)
    {
        if (request.PatientId <= 0)
        {
            return BadRequest("Valid patientId is required.");
        }

        if (!TryGetHealthCareProviderId(out var healthCareProviderId))
        {
            return Unauthorized("Invalid or missing HealthCareProviderId claim in token.");
        }

        var response = await sinaManager.OpenOrResumeSessionAsync(request.PatientId, healthCareProviderId, ct);
        return Ok(response);
    }

    [HttpPost("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<SinaTurnResponse>> SendMessage(Guid sessionId, [FromBody] SendSinaMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Message content is required.");
        }

        try
        {
            return Ok(await sinaManager.SendUserMessageAsync(sessionId, request.Content, ct));
        }
        catch (SinaUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> CloseSession(Guid sessionId, CancellationToken ct)
    {
        await sinaManager.CloseSessionAsync(sessionId, ct);
        return NoContent();
    }

    private bool TryGetHealthCareProviderId(out int healthCareProviderId)
    {
        return int.TryParse(User.FindFirst("HealthCareProviderId")?.Value, out healthCareProviderId)
            && healthCareProviderId > 0;
    }
}

public record OpenSinaSessionRequest(int PatientId);

public record SendSinaMessageRequest(string Content);
