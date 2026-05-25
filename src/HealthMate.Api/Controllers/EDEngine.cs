using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HealthMate.Application.Manager.MachineLearningManager;
using HealthMate.Infrastructure.DTO.MachineLearningDto;
using HealthMate.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HealthMate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "PatientOnly")]
    public class EDEngineController : ControllerBase
    {
        private readonly IMachineLearningManager _machineLearningManager;
        private readonly IValidationRepo _validationRepo;

        public EDEngineController(IValidationRepo validationRepo, IMachineLearningManager machineLearningManager)
        {
            _validationRepo = validationRepo;
            _machineLearningManager = machineLearningManager;
        }

        [HttpGet("check")]
        public async Task<IActionResult> Check()
        {
            
            if (!int.TryParse(User.FindFirst("PatientId")?.Value, out int patientId))
            {
                return Unauthorized("Invalid or missing PatientId claim in token.");
            }

            if(!await _validationRepo.CheckPatientId(patientId)){
                return BadRequest("Wrong patient"); 
            }
            
            var result = await _machineLearningManager.CheckAnimea(patientId);
            return Ok(result);
        }
    }
}