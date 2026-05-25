using HealthMate.Application.Manager.HealthRecordManager;
using HealthMate.Infrastructure.DTO.HealthRecordDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Authorize(policy: "PatientOrHealthCareProvider")]
	[ApiController]
    [Route("api/[controller]")]
    public class HealthRecordController : ControllerBase
    {
        private readonly IHealthRecordManager _healthRecordManager;

        public HealthRecordController(IHealthRecordManager healthRecordManager)
        {
            _healthRecordManager = healthRecordManager;
        }

        [HttpGet("summary/{patientId}")]
        public async Task<IActionResult> GetHealthRecordSummary(int patientId)
        {
            try
            {
                var result = await _healthRecordManager.GetAllHealthRecordSummary(patientId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("prescription-details/{prescriptionId}")]
        public async Task<IActionResult> GetPrescriptionDetails(int prescriptionId)
        {
            try
            {
                var result = await _healthRecordManager.GetPrescriptionDetailsAsync(prescriptionId);
                if (result == null)
                    return NotFound("Prescription not found");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("medical-image-details/{medicalImageId}")]
        public async Task<IActionResult> GetMedicalImageDetails(int medicalImageId)
        {
            try
            {
                var result = await _healthRecordManager.GetMedicalImageDetailsAsync(medicalImageId);
                if (result == null)
                    return NotFound("Medical image not found");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("lab-test-details/{labTestId}")]
        public async Task<IActionResult> GetLabTestDetails(int labTestId)
        {
            try
            {
                var result = await _healthRecordManager.GetLabTestDetailsAsync(labTestId);
                if (result == null)
                    return NotFound("Lab test not found");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("encounter-details/{encounterId}")]
        public async Task<IActionResult> GetEncounterDetails(int encounterId)
        {
            try
            {
                var result = await _healthRecordManager.GetEncounterDetailsAsync(encounterId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
