using System.Text;
using System.Text.RegularExpressions;
using EndEncounterDto;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO;
using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.EndEcnounterDto;
using HealthMate.Infrastructure.DTO.HealthCareProviderDto;
using HealthMate.Infrastructure.DTO.LabTestDto;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.HealthCareProviderRepos
{
	public class HealthCareProviderRepo : GenericRepository<HealthCareProvider>,IHealthCareProviderRepo
	{
		private readonly HealthMateContext _context;
		private readonly IDbContextFactory<HealthMateContext> _contextFactory; // parallel
		private readonly ILabTestAttributeRepo _LabTestRepo;
		
        public HealthCareProviderRepo(HealthMateContext context, ILabTestAttributeRepo LabTestRepo, IDbContextFactory<HealthMateContext> contextFactory) : base(context) 
        {
            _context= context;
			_LabTestRepo = LabTestRepo;
			_contextFactory = contextFactory;
		}

		public async Task<string> GetHealthCareProviderImageUrl(int healthCareProviderId){
			await using var context = await _contextFactory.CreateDbContextAsync();
			var imageUrl = await context.HealthCareProviders
				.Where(h => h.HealthCareProvider_Id == healthCareProviderId)
				.Select(h => h.ApplicationUser.ImageUrl)
				.FirstOrDefaultAsync();

			if (imageUrl == null)
			{
				throw new KeyNotFoundException($"Image URL not found for healthcare provider with ID {healthCareProviderId}");
			}

			return imageUrl;
		}

		public async Task<string> GetApplicationUserId(int healthCareProviderId){
			await using var context = await _contextFactory.CreateDbContextAsync();
			var applicationUserId = await context.HealthCareProviders
				.Where(h => h.HealthCareProvider_Id == healthCareProviderId)
				.Select(h => h.ApplicationUserId)
				.FirstOrDefaultAsync();

			if (applicationUserId == null)
			{
				throw new KeyNotFoundException($"Application User ID not found for healthcare provider with ID {healthCareProviderId}");
			}

			return applicationUserId;
		}
        public async Task<string> GetHealthCareProviderNameById(int healthCareProviderId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var name = await context.HealthCareProviders
				.Where(h => h.HealthCareProvider_Id == healthCareProviderId)
				.Select(n => n.ApplicationUser.First_Name + " " + n.ApplicationUser.Last_Name)
				.FirstOrDefaultAsync();

			if (name == null)
			{
				throw new KeyNotFoundException($"Healthcare provider with ID {healthCareProviderId} not found");
			}

			return name;
		}

		public async Task<string> GetHealthCareProviderSpecialization(int healthCareProviderId){
			await using var context = await _contextFactory.CreateDbContextAsync();
			var specialization = await context.HealthCareProviders
				.Where(e => e.HealthCareProvider_Id == healthCareProviderId)
				.Select(e => e.Specialization)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return specialization ?? "Unknown";
		}
		public async Task<IEnumerable<EncounterTableSummaryReadDto>> GetRecentEncountersOrderedAsync(int healthCareProviderId)
		{

			await using var context = await _contextFactory.CreateDbContextAsync();
			// Step 1: Retrieve and filter the data
			var rawData = await context.Encounters
				.Where(e => e.HealthCareProviderId == healthCareProviderId)
				.OrderByDescending(e => e.EndDate)
				.Select(e => new
				{
					PatientUserId = e.Patient.ApplicationUserId,
					PatientNationalId = e.Patient.NationalId,
					EncounterId = e.Encounter_Id,
					EncounterDate = e.EndDate,
					Diagnosis = e.Conditions.FirstOrDefault()!.Disease.Display_Name
				})
				.ToListAsync();

			var patientUserIds = rawData
				.Select(data => data.PatientUserId)
				.Where(static id => !string.IsNullOrWhiteSpace(id))
				.Distinct()
				.ToArray();
			var patientUsers = await context.Users
				.Where(user => patientUserIds.Contains(user.Id))
				.ToDictionaryAsync(user => user.Id);

			// Step 2: Map the raw data to EncounterSummary records
			var encounterSummaries = rawData.Select(data => new EncounterTableSummaryReadDto
			{
				EncounterId = data.EncounterId,
				Patient_Name = data.PatientUserId is not null && patientUsers.TryGetValue(data.PatientUserId, out var user)
					? user.First_Name + " " + user.Last_Name
					: "No Data",
				Patient_Id = data.PatientNationalId.Value,
				EncounterDate = DateOnly.FromDateTime(data.EncounterDate),
				Diagnosis = data.Diagnosis
			});

			return encounterSummaries;
		}

		public async Task<int> GetTheCountOfPatientsDoctorEncountered(int healthCareProviderId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			return await context.Encounters.Where(c => c.HealthCareProviderId == healthCareProviderId).Select(c => c.PatientId).Distinct().CountAsync();
		}

		public async Task<int> GetTheCountOfTodayEncounters(int healthCareProviderId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			return await context.Encounters.Where(c => c.HealthCareProviderId == healthCareProviderId && c.StartDate == DateTime.Today).CountAsync();
		}

		public async Task<int> GetTheCountOfTotalEncounters(int healthCareProviderId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			return await context.Encounters.Where(c => c.HealthCareProviderId == healthCareProviderId).CountAsync();
		}

		public async Task<int> AddEncounterAndReturnEncounterId(EndEncounterEncounterAddDto encounterData,int PatientId, int HealthCareProvider){

			var encounterLocation = await _context.HealthCareProviders.Where(h => h.HealthCareProvider_Id == HealthCareProvider).Select(h => h.City + " / " + h.Street).AsNoTracking()
				.FirstOrDefaultAsync() ?? "NO DATA";

			var encounter = new Encounter
			{
				PatientId = PatientId,
				HealthCareProviderId = HealthCareProvider,
				StartDate = encounterData.StartDate,
				EndDate = encounterData.EndDate,
				Location = encounterLocation,
				Reason_To_Visit = encounterData.Reason_To_Visit,
				Treatment_Plan = encounterData.Treatment_Plan,
				Note = encounterData.Note,
				isDeleted = false
			};

			await _context.Encounters.AddAsync(encounter);
			await _context.SaveChangesAsync();
			return encounter.Encounter_Id;
		}

		#region End Encounter Functionality
		private async Task CreateCondition(EndEncounterConditionAddDto conditionDto, int encounterId, int patientId)
		{
			var condition = new Condition
			{
				EncounterId = encounterId,
				Disease_Id = conditionDto.DiseasesId,
				ClinicalStatus = conditionDto.ClinicalStatus,
				DateRecorded = conditionDto.DateRecorded,
				PaientId = patientId,
				Severity = conditionDto.Severity,
				BodySiteId = conditionDto.BodySite,
				Note = conditionDto.Note
			};

			await _context.Conditions.AddAsync(condition);
		}

		private async Task CreateObservation(EndEncounterObservationAddDto observationData, int patientId){
			var now = DateTime.UtcNow.AddHours(2);
			var observation = new Observation{
				PatientId = patientId,
				BodySiteId = observationData.BodySiteId,
				Category = observationData.Category,
				Code = observationData.Code,
				CodeDisplayName = observationData.ObservationName.ToLower(),
				Interpertation = observationData.Interpertation,
				DateOfObservation = now,
				ValueQuanitity = observationData.ValueQuanitity,
				NameIdentifier = $"{patientId}-{Regex.Replace(observationData.ObservationName.ToLower(),@"\s+", "")}-{now:yyyyMMddHHmmsss}",
				ValueUnit = observationData.ValueUnit
			};

			await _context.Observations.AddAsync(observation);
		}
		// i use the patient id to compose the nameIdentifier but this has security risks by exposing the internal id of the patient to healthcare provider
		// so i should use the national id this will be more secure بس مكسل
		private async Task CreatePrescription(EndEncounterPrescriptionAddDto prescriptionDto, int patientId, int encounterId, string publisher)
		{	
			var now = DateTime.UtcNow.AddHours(2);

			var prescription = new Prescription
			{
				Publisher =publisher,
				PatientId = patientId,
				PrescriptionDate = now,
				EncounterId = encounterId,
				NameIdentifier = $"{patientId}-{Regex.Replace(publisher, @"\s+", "")}-{now:yyyyMMddHHmmss}",
				PatientMedicines = await CreatePatientMedicines(prescriptionDto.Medicines, patientId)
			};

			await _context.Prescriptions.AddAsync(prescription);
		}

		private async Task<List<PatientMedicine>> CreatePatientMedicines(
			ICollection<PatientMedicineAddDto> medicines, 
			int patientId)
		{
			return medicines.Select(medicineDto => new PatientMedicine
			{
				PatientId = patientId,
				MedicineId = medicineDto.MedicineId,
				FrequencyInHours = medicineDto.FrequencyInHours,
				DurationInDays = medicineDto.DurationInDays,
				IsPrescribed = medicineDto.IsPrescribed,
				AddedDate = DateTime.UtcNow.AddHours(2),
				Dosage = medicineDto.Dosage
			}).ToList();
		}

		private async Task<List<LabTestResult>> CreateLabTestValues(int LabTestId,ICollection<LabTestResultDto> Results){

			var labTestResults = new List<LabTestResult>();
    
			foreach (var resultData in Results)
			{
				var attributeId = await _LabTestRepo.GetIdByNameAsync(resultData.AttributeName);
				var result = new LabTestResult
				{
					LabTestId = LabTestId,
					LabTestAttributeId = attributeId,
					Value = resultData.Value
				};
				labTestResults.Add(result);
			}

			return labTestResults;
		}
		private async Task CreateLabTest(int patientId, EndEncounterLabTestAddDto labTestDto)
		{
				var now = DateTime.UtcNow.AddHours(2);
				var identifier = $"{patientId}-{Regex.Replace(labTestDto.LabTestName.ToLower(), @"\s+", "")}-{now:yyyyMMddHHmmss}";
				// Create New LabTest
				var labTest = new LabTest
				{
					patientId = patientId,
					LabTestName = labTestDto.LabTestName,
					RecordedTime = labTestDto.RecordedTime,
					NameIdentifier = identifier,
					Note = labTestDto.Note,
				};

				// Add and save to get the generated LabTestId
				await _context.LabTests.AddAsync(labTest);
				await _context.SaveChangesAsync();

				// Now create results using the generated LabTestId
				var labTestResults = await CreateLabTestValues(labTest.LabTestId, labTestDto.Results);
				labTest.LabTestResults = labTestResults;

				// i create and save changes of the LabTest first to automatically get the LabTestId
		}

		

		public async Task<bool> EndEncounter(EndEncounter endEcounterDto, int PatientId, int HealthcareProviderId)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// 1. Validate all required entities exist
				// use validation repo to validate the healthcareProviderId and the patient Id
				// the validation shuold be in the controller 

				// 2. Create and save encounter
				var encounterId = await AddEncounterAndReturnEncounterId(endEcounterDto.Encounter, PatientId, HealthcareProviderId);

				// 3. Create condition if provided
				if (endEcounterDto.Condition != null)
				{
					await CreateCondition(endEcounterDto.Condition, encounterId, PatientId);
				}

				var prescriptionPub = await _context.HealthCareProviders.Where(h => h.HealthCareProvider_Id == HealthcareProviderId).AsNoTracking()
					.Select(h => "DR/" + h.ApplicationUser.First_Name + " " + h.ApplicationUser.Last_Name).FirstOrDefaultAsync() ?? "NO DATA";

				// 4. Create prescription if provided
				if (endEcounterDto.Prescription != null)
				{
					await CreatePrescription(endEcounterDto.Prescription, PatientId, encounterId, prescriptionPub);
				}

				// 5. Create LabTest If Provided
				if(endEcounterDto.LabTests != null){
					await CreateLabTest(PatientId,endEcounterDto.LabTests);
				} 
				
				// 6. Create Observations
				if(endEcounterDto.Observations != null){
					foreach(var observation in endEcounterDto.Observations){
						await CreateObservation(observation,PatientId);
					}
				}

				// 6. Save all changes and commit transaction
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
				return true;
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				// Log the inner exception
				var innerException = ex.InnerException?.Message ?? ex.Message;
				throw new Exception($"Database error: {innerException}");
			}
		}

		#endregion

		#region Start Encounter Functionality
		public async Task<int> GetPatientIdByPatientNationalId(string PatientNationalId){
			var nationalId = NationalId.FromTrusted(PatientNationalId);
			var patient = await _context.Patients
				.FirstOrDefaultAsync(patient => patient.NationalId == nationalId);
			return patient.Patient_Id;
		}
		#endregion

		public async Task<List<int>> GetLast7DaysEncountersCountAsync(int healthCareProviderId)
		{
			var today = DateTime.Today;
			var last7Days = Enumerable.Range(0, 7)
				.Select(offset => today.AddDays(-offset))
				.ToList();

			var result = new List<int>();

			await using var context = await _contextFactory.CreateDbContextAsync();

			foreach (var day in last7Days)
			{
				var count = await context.Encounters
					.Where(e => e.HealthCareProviderId == healthCareProviderId &&
							e.StartDate.Date == day.Date)
					.CountAsync();

				result.Add(count);
			}

			return result;
		}
	
		public async Task<List<ConditionFrequencyDto>> GetTop5FrequentConditionsWithCountAsync(int healthCareProviderId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			var topConditions = await context.Encounters
				.Where(e => e.HealthCareProviderId == healthCareProviderId)
				.SelectMany(e => e.Conditions)
				.GroupBy(c => c.Disease.Display_Name)
				.OrderByDescending(g => g.Count())
				.Take(5)
				.Select(g => new ConditionFrequencyDto
				{ 
					ConditionName = g.Key, 
					Frequency = g.Count() 
				})
				.ToListAsync();

			return topConditions;
		}
	}
}
