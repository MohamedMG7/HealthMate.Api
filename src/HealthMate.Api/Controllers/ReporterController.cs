using HealthMate.Application.Abstractions.Validation;
using HealthMate.Application.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;

namespace HealthMate.Api.Controllers{
    [Authorize(policy: "HealthCareProviderOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class ReporterController : ControllerBase{
        private readonly IValidationRepo _validationRepo;
        private readonly IReporter _reporter;

        public ReporterController(IValidationRepo validationRepo, IReporter reporter)
        {
            _validationRepo = validationRepo;
            _reporter = reporter;
        }

        [HttpGet]
        [Route("Traffic-Report")]
        public async Task<IActionResult> TrafficReport(int healthcareProviderId){
            // validation
            if(!await _validationRepo.CheckHealthcareProviderId(healthcareProviderId)){
                return StatusCode(StatusCodes.Status404NotFound);
            }   

            var result = await _reporter.generateTrafficReport(healthcareProviderId);
            return StatusCode(StatusCodes.Status200OK, result);
        }
    }
}
