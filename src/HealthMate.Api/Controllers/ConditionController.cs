using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Manager.ConditionManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ConditionController : ControllerBase
	{
        private readonly IConditionManager _conditionManager;
        public ConditionController(IConditionManager conditionManager)
        {
            _conditionManager = conditionManager;
        }

		[Authorize(policy:"HealthCareProviderOnly")]
        [HttpGet]
        public IActionResult GetAllConditions()
        {
			var Conditions = _conditionManager.GetAllConditions();

			if (Conditions == null || !Conditions.Any())
			{
				return NotFound("No Conditions Found");
			}

			return Ok(Conditions);
		}

		[Authorize(policy:"PatientOrHealthCareProvider")]
		[HttpPost]
		[Obsolete("Use POST /api/Encounter/{encounterId}/conditions; will be removed after Slice 5.")]
		public IActionResult AddCondition(ConditionAddDto condition) { 
			_conditionManager.AddCondition(condition);	
			return Ok("Added Succesfully");
		}

		[Authorize(policy:"PatientOrHealthCareProvider")]
		[HttpGet]
		[Route("{id}")]
		public IActionResult GetConditionById(int id)
		{
			var Condition = _conditionManager.GetCondition(id);
			if (Condition == null)
			{
				return NotFound("No Condition Found by this Id");
			}
			return Ok(Condition);
		}

		[Authorize(policy:"PatientOrHealthCareProvider")]
		[HttpDelete]
		[Route("{id}")]
		public ActionResult DeleteConditionById(int id)
		{
			_conditionManager.DeleteCondition(id);
			return Ok("Deleted Succesfully");
		}
	}
}
