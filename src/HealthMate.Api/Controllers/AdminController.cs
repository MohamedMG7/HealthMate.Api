using HealthMate.Application.Common;
using HealthMate.Application.Identity.Contracts;
using HealthMate.Application.Patients.Commands;
using HealthMate.Application.Patients.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	
	[Authorize(policy:"AdminOnly")]
	[Route("api/[controller]")]
	[ApiController]
	public class AdminController : ControllerBase
	{
		private readonly IHandlerDispatcher _dispatcher;

		public AdminController(IHandlerDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		[HttpGet]
		public async Task<IActionResult> GetAllPatientsToBeVerified(CancellationToken ct)
		{
			var patients = await _dispatcher.DispatchAsync(new ListPatientsToVerifyQuery(), ct);

			if (!patients.Any())
			{
				return NotFound("No Patients Found");
			}

			return Ok(patients);
		}

		[HttpPost("approve-reject")]
		public async Task<IActionResult> ApproveOrRejectPatient([FromBody] AdminApprovalDto approvalDto, CancellationToken ct)
		{
			await _dispatcher.DispatchAsync(new VerifyPatientCommand(
				approvalDto.PatientId,
				approvalDto.IsApproved,
				approvalDto.RejectionReason), ct);

			return Ok("Operation Succeded.");
		}

		
		[HttpGet("test")]
		public IActionResult Test()
		{
			return Ok("Admin endpoint works");
		}
	}
}
