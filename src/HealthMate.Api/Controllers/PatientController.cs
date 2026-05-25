using HealthMate.Infrastructure.DTO.PatientDto.AnimalPatientDtos;
using HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos;
using HealthMate.Infrastructure.DTO;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Application.Manager.UsersManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PatientController : ControllerBase
	{
		private readonly IPatientManager _patientManager;
		public PatientController(IPatientManager patientManager)
		{
			_patientManager = patientManager;
		}

		[Authorize(policy:"AdminOnly")]
		[HttpGet]
		public IActionResult GetAll()
		{
			var Patients = _patientManager.GetAllHumanPatients();

			if (Patients == null || !Patients.Any())
			{
				return NotFound("No Patients Found");
			}

			return Ok(Patients);
		}

		[Authorize(policy:"AdminOnly")]
		[HttpGet]
		[Route("VerifiedPatients")]
		public IActionResult GetAllVerified()
		{
			var activeHumanPatients = _patientManager.GetAllVerifiedHumanPatients();

			if (activeHumanPatients == null || !activeHumanPatients.Any())
			{
				return NotFound("No active patients found.");
			}

			return Ok(activeHumanPatients);
		}

		
		[HttpPost]
		public IActionResult AddHumanPatient(HumanPatientAddDto HumanPatient) 
		{
			_patientManager.AddHumanPatient(HumanPatient);
			return Ok($"Patient Added Succesfully");
		}
		
		[Authorize(policy:"PatientOnly")]
		[HttpPost]
		[Route("Add_Animal")]
		public IActionResult AddAnimalPatient(AnimalPatientAddDto AnimalPatient)
		{
			_patientManager.AddAnimalPatient(AnimalPatient);
			return Ok($"Animal Added Succesfully");
		}

		[Authorize(policy: "PatientOnly")]
		[HttpGet]
		[Route("GetPatientDashboard")]
		public async Task<IActionResult> GetPatientDashboard([FromQuery] int patientId)
		{
			// Validate patientId and periodInDays
			if (patientId <= 0)
			{
				return BadRequest("Invalid patient ID or period in days.");
			}

			try
			{
				var dashboardData = await _patientManager.GetMobilePatientDashboardDataAsync(patientId);
				if (dashboardData != null)
				{
					return Ok(dashboardData);
				}
				return NotFound("No dashboard data found for the specified patient.");
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, $"Internal Server Error: {ex.Message}");
			}
		}
		
		//[Authorize(policy:"PatientOnly")]
		//[HttpPost]
		//[Route("AddMedicine")]
		//public async Task<IActionResult> AddPatientMedicine(PatientMedicineAddDto medicineAddDto)
		//{
		//	if (medicineAddDto == null || 
		//		medicineAddDto.PatientId <= 0 || 
		//		medicineAddDto.MedicineId <= 0 ||
		//		medicineAddDto.FrequencyInHours <= 0 ||
		//		medicineAddDto.DurationInDays <= 0)
		//	{
		//		return BadRequest("Invalid medicine data");
		//	}

		//	var result = await _patientManager.AddPatientMedicine(medicineAddDto);
			
		//	if (result)
		//	{
		//		return Ok("Medicine added successfully");
		//	}
			
		//	return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add medicine");
		//}
	}
}
