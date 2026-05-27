using HealthMate.Application.Manager.DiseaseManager;
using HealthMate.Application.Clinical.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers{
    [Route("api/[controller]")]
    [ApiController]
    public class DiseaseController : ControllerBase{
        private readonly IDiseaseManager _diseaseManager;
        public DiseaseController(IDiseaseManager diseaseManager)
        {
            _diseaseManager = diseaseManager;
        }

        [HttpGet("get-diseases")]
        public async Task<IActionResult> GetDiseasesNameAndId()
        {
            var diseases = await _diseaseManager.getDiseasesNameAndId();
            return Ok(diseases);
        }
    }
}