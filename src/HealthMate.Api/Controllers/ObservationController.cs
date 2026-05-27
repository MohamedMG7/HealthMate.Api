using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Manager.ObservationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HealthMate.Api.Controllers
{
	//[Authorize(policy:"PatientOrHealthCareProvider")]
	[Route("api/[controller]")]
	[ApiController]
	public class ObservationController : ControllerBase
	{
		private readonly IObservationManager _observationManager;
		public ObservationController(IObservationManager observationManager)
		{
			_observationManager = observationManager;
		}

		[HttpGet]
		public IActionResult GetAllObservations()
		{
			var Observations = _observationManager.GetAllObservations();

			if (Observations == null || !Observations.Any())
			{
				return NotFound("No Observations Found");
			}

			return Ok(Observations);
		}

		[HttpPost]
		public IActionResult AddObservation(ObservationAddDto observation)
		{
			_observationManager.AddObservation(observation);
			return Ok("Added Succesfully");
		}

		[HttpGet]
		[Route("{id}")]
		public IActionResult GetEncounterById(int id)
		{
			var Observation = _observationManager.GetObservation(id);
			if (Observation == null)
			{
				return NotFound("No Observation Found by this Id");
			}
			return Ok(Observation);
		}

		[HttpDelete]
		[Route("{id}")]
		public ActionResult DeleteEncounterById(int id)
		{
			_observationManager.DeleteObservation(id);
			return Ok("Deleted Succesfully");
		}
	}
}
