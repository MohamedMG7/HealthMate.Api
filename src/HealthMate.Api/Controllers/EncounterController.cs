using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Manager.ConditionManager;
using HealthMate.Application.Manager.EncounterManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Authorize(policy:"PatientOrHealthCareProvider")]
	[Route("api/[controller]")]
	[ApiController]
	public class EncounterController : ControllerBase
	{
		private readonly IEncounterManager _encounterManager;
		public EncounterController(IEncounterManager encounterManager)
		{
			_encounterManager = encounterManager;
		}

		[HttpGet]
		public IActionResult GetAllEncounters()
		{
			var Conditions = _encounterManager.GetAllEncounters();

			if (Conditions == null || !Conditions.Any())
			{
				return NotFound("No Encounters Found");
			}

			return Ok(Conditions);
		}

		[HttpPost]
		public IActionResult AddEncounter(EncounterAddDto encounter)
		{
			_encounterManager.AddEncounter(encounter);
			return Ok("Added Succesfully");
		}

		[HttpGet]
		[Route("{id}")]
		public IActionResult GetEncounterById(int id)
		{
			var Encounter = _encounterManager.GetEncounter(id);
			if (Encounter == null)
			{
				return NotFound("No Encounter Found by this Id");
			}
			return Ok(Encounter);
		}

		[HttpDelete]
		[Route("{id}")]
		public ActionResult DeleteEncounterById(int id)
		{
			_encounterManager.DeleteEncounter(id);
			return Ok("Deleted Succesfully");
		}
	}
}
