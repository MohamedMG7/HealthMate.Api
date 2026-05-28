using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Common;
using HealthMate.Application.Encounters.Commands;
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
		private readonly IHandlerDispatcher _dispatcher;
		private readonly IEncounterManager _encounterManager;
		public EncounterController(IHandlerDispatcher dispatcher, IEncounterManager encounterManager)
		{
			_dispatcher = dispatcher;
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
		[Obsolete("Use POST /api/Encounter/start; will be removed after Slice 5.")]
		public IActionResult AddEncounter(EncounterAddDto encounter)
		{
			_encounterManager.AddEncounter(encounter);
			return Ok("Added Succesfully");
		}

		[Authorize(Policy = "HealthCareProviderOnly")]
		[HttpPost("start")]
		public async Task<IActionResult> StartEncounter(
			StartEncounterRequestDto request,
			CancellationToken ct)
		{
			var result = await _dispatcher.DispatchAsync(
				new StartEncounterCommand(
					request.PatientId,
					request.HealthCareProviderId,
					request.ReasonToVisit),
				ct);

			return Created($"/api/Encounter/{result.EncounterId}", result);
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
