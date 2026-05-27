using Microsoft.AspNetCore.Mvc;
using HealthMate.Application.Abstractions.Validation;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Manager.HealthCareProviderManager;
using HealthMate.Application.Providers.Contracts;
using HealthMate.Application.Manager.HealthRecordManager;
using Microsoft.AspNetCore.Authorization;

namespace HealthMate.Api.Controllers
{
	[Authorize(policy:"HealthCareProviderOnly")]
	[Route("api/[controller]")]
	[ApiController]
	public class HealthCareProviderController : ControllerBase
	{
		private readonly IHealthCareProviderManager _HealthCareProviderManager;
		private readonly IHealthRecordManager _HealthRecordManager;
		private readonly IValidationRepo _ValidationRepo;
        public HealthCareProviderController(IValidationRepo ValidationRepo, IHealthCareProviderManager HealthCareProviderManager, IHealthRecordManager HealthRecordManager)
        {
			_HealthCareProviderManager = HealthCareProviderManager;
			_HealthRecordManager = HealthRecordManager;
			_ValidationRepo = ValidationRepo;
        }
		
		[HttpGet]
		[Route("ClinicDashboard")]
		public async Task<IActionResult> ClinicDashBoard(int HealthCareProviderId) {
			
			var dashboardData = await _HealthCareProviderManager.GetClinicalDashboardData(HealthCareProviderId);

			return Ok(dashboardData);
		}

		[HttpGet]
		[Route("HealthRecords")]
		public async Task<IActionResult> HealthRecords(int patientId)
		{
			var Records = await _HealthRecordManager.GetAllHealthRecords(patientId);

			if(Records == null){
				return NotFound("No Records Found");
			}

			return Ok(Records);
		}
		
		[HttpPost]
		[Route("EndEncounter/{patientId}/{healthcareProviderId}")]
		public async Task<IActionResult> EndEncounter([FromBody] EndEncounter EndEncounterData,int patientId, int healthcareProviderId)
		{
			try
			{
				var result = await _HealthCareProviderManager.EndEncounter(EndEncounterData,patientId,healthcareProviderId);
				if(result){
					return StatusCode(StatusCodes.Status201Created);
				}
				return StatusCode(StatusCodes.Status400BadRequest,"Something Wrong Happened");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		[HttpGet]
		[Route("StartEncounter")]
		public async Task<IActionResult> StartEncounter([FromQuery]string patientNationalId){

			//patietnNationalId Validation
			var patientExists = await _ValidationRepo.CheckPatientNationalId(patientNationalId);
			if (!patientExists)
			{
				return NotFound("Patient with the provided National ID does not exist.");
			}

			try{
				var result = await _HealthCareProviderManager.StartEncounter(patientNationalId);
				if(result != null){
					return StatusCode(StatusCodes.Status200OK,result);
				}
				return StatusCode(StatusCodes.Status400BadRequest,"Wrong Id");
			}catch(Exception ex){
				return StatusCode(StatusCodes.Status500InternalServerError,$"Internal Server Error: {ex.Message}");
			}
		}

		
	}
}
