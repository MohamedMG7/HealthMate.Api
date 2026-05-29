using HealthMate.Application.Manager.ObservationManager;
using Microsoft.AspNetCore.Mvc;

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
	}
}
