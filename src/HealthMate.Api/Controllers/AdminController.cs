using HealthMate.Infrastructure.DTO.AdminDto;
using HealthMate.Application.Manager.AdminManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	
	[Authorize(policy:"AdminOnly")]
	[Route("api/[controller]")]
	[ApiController]
	public class AdminController : ControllerBase
	{
		private readonly IAdminManager _AdminManager;
		public AdminController(IAdminManager AdminManager)
		{
			_AdminManager = AdminManager;
		}

		[HttpGet]
		public IActionResult GetAllPatientsToBeVerified()
		{
			var Patients = _AdminManager.GetPatients();

			if (Patients == null || !Patients.Any())
			{
				return NotFound("No Patients Found");
			}

			return Ok(Patients);
		}

		[HttpPost("approve-reject")]
		public IActionResult ApproveOrRejectPatient([FromBody] AdminApprovalDto approvalDto)
		{
			try
			{
				_AdminManager.ApproveOrRejectPatient(approvalDto);
				return Ok("Operation Succeded.");
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { Message = ex.Message });
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { Message = ex.Message });
			}
			catch (InvalidOperationException ex) {
				return BadRequest(new { Message = ex.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
			}
		}

		
		[HttpGet("test")]
		public IActionResult Test()
		{
			return Ok("Admin endpoint works");
		}
	}
}
