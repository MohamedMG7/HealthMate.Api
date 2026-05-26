using HealthMate.Infrastructure.DTO.PatientDto.AnimalPatientDtos;
using HealthMate.Application.Common;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Application.Patients.Commands;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Application.Patients.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PatientController : ControllerBase
	{
		private readonly IHandlerDispatcher _dispatcher;
		private readonly IPatientManager _patientManager;

		public PatientController(IHandlerDispatcher dispatcher, IPatientManager patientManager)
		{
			_dispatcher = dispatcher;
			_patientManager = patientManager;
		}

		[Authorize(policy:"AdminOnly")]
		[HttpGet]
		public async Task<IActionResult> GetAll(CancellationToken ct)
		{
			var patients = await _dispatcher.DispatchAsync(new ListPatientsQuery(), ct);

			if (!patients.Any())
			{
				return NotFound("No Patients Found");
			}

			return Ok(patients);
		}

		[Authorize(policy:"AdminOnly")]
		[HttpGet]
		[Route("VerifiedPatients")]
		public async Task<IActionResult> GetAllVerified(CancellationToken ct)
		{
			var activeHumanPatients = await _dispatcher.DispatchAsync(new ListVerifiedPatientsQuery(), ct);

			if (!activeHumanPatients.Any())
			{
				return NotFound("No active patients found.");
			}

			return Ok(activeHumanPatients);
		}

		
		[HttpPost]
		public async Task<IActionResult> AddHumanPatient(HumanPatientAddDto humanPatient, CancellationToken ct)
		{
			var result = await _dispatcher.DispatchAsync(new RegisterHumanPatientCommand(
				humanPatient.NationalId,
				humanPatient.NationalIdImageUrl,
				humanPatient.BirthDate,
				humanPatient.Gender,
				humanPatient.Governorate,
				humanPatient.City,
				humanPatient.ApplicationUserId,
				humanPatient.Weight,
				humanPatient.Height), ct);

			return Created($"/api/Patient/{result.PatientId}", result);
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
		public async Task<IActionResult> GetPatientDashboard([FromQuery] int patientId, CancellationToken ct)
		{
			// Validate patientId and periodInDays
			if (patientId <= 0)
			{
				return BadRequest("Invalid patient ID or period in days.");
			}

			var dashboardData = await _dispatcher.DispatchAsync(new GetPatientDashboardQuery(patientId), ct);
			return Ok(dashboardData);
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
