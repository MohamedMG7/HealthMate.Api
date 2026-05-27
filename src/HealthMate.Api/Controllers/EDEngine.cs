using HealthMate.Application.Abstractions.Validation;
using HealthMate.Application.Manager.MachineLearningManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "PatientOnly")]
public class EDEngineController : ControllerBase
{
    private readonly IMachineLearningManager _machineLearningManager;
    private readonly IValidationRepo _validationRepo;
    private readonly ILogger<EDEngineController> _logger;

    public EDEngineController(
        IValidationRepo validationRepo,
        IMachineLearningManager machineLearningManager,
        ILogger<EDEngineController> logger)
    {
        _validationRepo = validationRepo;
        _machineLearningManager = machineLearningManager;
        _logger = logger;
    }

    [HttpGet("check")]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("PatientId")?.Value, out int patientId))
        {
            return Unauthorized("Invalid or missing PatientId claim in token.");
        }

        if (!await _validationRepo.CheckPatientId(patientId))
        {
            return BadRequest("Wrong patient");
        }

        try
        {
            var result = await _machineLearningManager.CheckAnimea(patientId, cancellationToken);
            return Ok(result);
        }
        catch (NoCbcDataException)
        {
            // Surface absence of data instead of silently returning a false negative.
            return BadRequest("No recent CBC test on file for this patient.");
        }
        catch (MlGatewayException ex)
        {
            _logger.LogWarning(ex, "ML service unavailable for patient check");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "ML service unavailable.");
        }
    }
}
