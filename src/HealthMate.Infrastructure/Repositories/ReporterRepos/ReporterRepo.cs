using System.Globalization;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Application.Admin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories{
    public class ReporterRepo : IReporterRepo{
        private readonly IDbContextFactory<HealthMateContext> _contextFactory;
        public ReporterRepo(IDbContextFactory<HealthMateContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<TrafficReportDto> getDataForTrafficReport(int healthCareProviderId){
            using var context = await _contextFactory.CreateDbContextAsync();

            var encounters = await context.Encounters
                .AsNoTracking()
                .Where(e => e.HealthCareProviderId == healthCareProviderId && !e.IsDeleted)
                .ToListAsync();

            var patientIds = encounters.Select(e => e.PatientId).Distinct().ToArray();
            var patientsById = await context.Patients
                .AsNoTracking()
                .Where(patient => patientIds.Contains(patient.Id))
                .ToDictionaryAsync(patient => patient.Id);

            var encounterIds = encounters.Select(e => e.Id).ToArray();
            var conditions = await context.Conditions
                .AsNoTracking()
                .Include(condition => condition.BodySite)
                .Include(condition => condition.Disease)
                .Where(condition => condition.EncounterId.HasValue && encounterIds.Contains(condition.EncounterId.Value))
                .ToListAsync();
            var conditionsByEncounterId = conditions
                .GroupBy(condition => condition.EncounterId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            var report = new TrafficReportDto();

            // 1. Patient Overview
            var distinctPatients = encounters
                .Select(e => patientsById.GetValueOrDefault(e.PatientId))
                .Where(patient => patient is not null)
                .DistinctBy(patient => patient!.Id)
                .Select(patient => patient!)
                .ToList();
            report.TotalPatients = distinctPatients.Count;

            report.NewPatientsPerYear = distinctPatients
                .GroupBy(p =>
                    context.Encounters
                        .Where(e => e.PatientId == p.Id)
                        .OrderBy(e => e.StartDate)
                        .Select(e => e.StartDate.Year)
                        .FirstOrDefault())
                .ToDictionary(g => g.Key, g => g.Count());

            report.AveragePatientsPerMonth = encounters
                .GroupBy(e => new { e.StartDate.Year, e.StartDate.Month })
                .Average(g => g.Select(e => e.PatientId).Distinct().Count());

            report.RepeatVisitRate = encounters
                .GroupBy(e => e.PatientId)
                .Average(g => g.Count());

            // Age groups
            var now = DateOnly.FromDateTime(DateTime.Now);
            report.PatientAgeGroups = distinctPatients
                .GroupBy(p =>
                {
                    var age = now.Year - p.BirthDate.Year;
                    if (age < 18) return "Under 18";
                    if (age <= 25) return "18-25";
                    if (age <= 40) return "26-40";
                    if (age <= 60) return "41-60";
                    return "60+";
                })
                .ToDictionary(g => g.Key, g => g.Count());

            report.GenderDistribution = distinctPatients
                .GroupBy(p => p.Gender.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            report.LocationDistribution = distinctPatients
                .GroupBy(p => $"{p.Governorate.Value} - {p.City.Value}")
                .ToDictionary(g => g.Key, g => g.Count());

            // 2. Encounter Analytics
            report.TotalEncounters = encounters.Count;

            report.EncountersPerYear = encounters
                .GroupBy(e => e.StartDate.Year)
                .ToDictionary(g => g.Key, g => g.Count());

            report.EncountersPerMonth = encounters
                .GroupBy(e => e.StartDate.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .ToDictionary(g => g.Key, g => g.Count());

            report.AverageEncounterDurationInMinutes = Math.Round(
                encounters.Average(e => (e.EndDate - e.StartDate).TotalMinutes),2);

            report.MostCommonVisitReasons = encounters
                .GroupBy(e => e.ReasonToVisit.Value)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var conditionCounts = encounters
                .Select(e => conditionsByEncounterId.TryGetValue(e.Id, out var encounterConditions) ? encounterConditions.Count : 0)
                .Where(count => count > 0)
                .ToArray();
            report.AverageConditionsPerEncounter = conditionCounts.Length == 0 ? 0 : conditionCounts.Average();

            // 3. Condition & Disease Insights
            var allConditions = conditions;

            report.SeverityDistribution = allConditions
                .GroupBy(c => c.Severity.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            report.ClinicalStatusDistribution = allConditions
                .GroupBy(c => c.ClinicalStatus.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            report.MostAffectedBodySites = allConditions
                .Where(c => c.BodySite != null)
                .GroupBy(c => c.BodySite.DisplayName)
                .ToDictionary(g => g.Key, g => g.Count());

            return report;

        }
    }
}
