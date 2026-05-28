using System.Text;
using System.Text.RegularExpressions;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Common;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Providers.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Prescriptions.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.HealthCareProviderRepos
{
	public class HealthCareProviderRepo : GenericRepository<HealthCareProvider>,IHealthCareProviderRepo
	{
		private readonly HealthMateContext _context;
		private readonly IDbContextFactory<HealthMateContext> _contextFactory; // parallel
		private readonly ILabTestAttributeRepo _LabTestRepo;
		private readonly IDateTimeProvider _clock;
		
		public HealthCareProviderRepo(HealthMateContext context, ILabTestAttributeRepo LabTestRepo, IDbContextFactory<HealthMateContext> contextFactory, IDateTimeProvider clock) : base(context)
        {
            _context= context;
			_LabTestRepo = LabTestRepo;
			_contextFactory = contextFactory;
			_clock = clock;
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
					PatientId = e.PatientId,
					EncounterId = e.Id,
					EncounterDate = e.EndDate,
				})
				.ToListAsync();

			var rawPatientIds = rawData.Select(data => data.PatientId).Distinct().ToArray();
			var patientsById = await context.Patients
				.Where(patient => rawPatientIds.Contains(patient.Id))
				.Select(patient => new
				{
					patient.Id,
					patient.ApplicationUserId,
					patient.NationalId
				})
				.ToDictionaryAsync(patient => patient.Id);

			var rawEncounterIds = rawData.Select(data => data.EncounterId).ToArray();
			var diagnosesByEncounterId = await (
				from condition in context.Conditions.AsNoTracking()
				join disease in context.Diseases.AsNoTracking() on condition.DiseaseId equals disease.Disease_Id
				where condition.EncounterId.HasValue && rawEncounterIds.Contains(condition.EncounterId.Value)
				group disease.Display_Name by condition.EncounterId!.Value into groupByEncounter
				select new
				{
					EncounterId = groupByEncounter.Key,
					Diagnosis = groupByEncounter.FirstOrDefault()
				})
				.ToDictionaryAsync(item => item.EncounterId, item => item.Diagnosis ?? "Unknown");

			var patientUserIds = rawData
				.Select(data => patientsById.GetValueOrDefault(data.PatientId)?.ApplicationUserId)
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
				Patient_Name = patientsById.GetValueOrDefault(data.PatientId)?.ApplicationUserId is { } patientUserId && patientUsers.TryGetValue(patientUserId, out var user)
					? user.First_Name + " " + user.Last_Name
					: "No Data",
				Patient_Id = patientsById.GetValueOrDefault(data.PatientId)?.NationalId.Value ?? "No Data",
				EncounterDate = DateOnly.FromDateTime(data.EncounterDate),
				Diagnosis = diagnosesByEncounterId.GetValueOrDefault(data.EncounterId, "Unknown")
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

			var encounter = Encounter.CreateLegacy(
				PatientId,
				HealthCareProvider,
				encounterData.StartDate,
				encounterData.EndDate,
				encounterLocation,
				encounterData.Reason_To_Visit,
				encounterData.Treatment_Plan,
				encounterData.Note);

			await _context.Encounters.AddAsync(encounter);
			await _context.SaveChangesAsync();
			return encounter.Id;
		}

		#region End Encounter Functionality
		private async Task CreateCondition(EndEncounterConditionAddDto conditionDto, int encounterId, int patientId)
		{
			var condition = Condition.Record(
				patientId,
				encounterId,
				conditionDto.DiseasesId,
				conditionDto.Severity,
				conditionDto.ClinicalStatus,
				conditionDto.DateRecorded,
				conditionDto.Note,
				_clock);

			await _context.Conditions.AddAsync(condition);
		}

		private async Task CreateObservation(EndEncounterObservationAddDto observationData, int patientId, int encounterId){
			var now = _clock.UtcNow.UtcDateTime;
			var observation = Observation.Record(
				patientId,
				encounterId,
				observationData.Category,
				observationData.Code,
				observationData.ObservationName.ToLower(),
				observationData.ValueQuanitity,
				observationData.ValueUnit,
				observationData.Interpertation,
				observationData.BodySiteId,
				now,
				null,
				_clock);

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
						await CreateObservation(observation,PatientId,encounterId);
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
			return patient.Id;
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
			
			var topConditions = await (
				from condition in context.Conditions.AsNoTracking()
				join disease in context.Diseases.AsNoTracking() on condition.DiseaseId equals disease.Disease_Id
				where condition.EncounterId.HasValue && context.Encounters
					.Any(encounter => encounter.Id == condition.EncounterId.Value && encounter.HealthCareProviderId == healthCareProviderId)
				group disease.Display_Name by disease.Display_Name into groupByDisease
				orderby groupByDisease.Count() descending
				select new ConditionFrequencyDto
				{
					ConditionName = groupByDisease.Key,
					Frequency = groupByDisease.Count()
				})
				.Take(5)
				.ToListAsync();

			return topConditions;
		}
	}
}
