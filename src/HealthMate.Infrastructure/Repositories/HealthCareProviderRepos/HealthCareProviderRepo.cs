using HealthMate.Application.Encounters.Contracts;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Providers.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.HealthCareProviderRepos
{
	public class HealthCareProviderRepo : GenericRepository<HealthCareProvider>,IHealthCareProviderRepo
	{
		private readonly HealthMateContext _context;
		private readonly IDbContextFactory<HealthMateContext> _contextFactory;

		public HealthCareProviderRepo(HealthMateContext context, IDbContextFactory<HealthMateContext> contextFactory) : base(context)
        {
            _context = context;
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
