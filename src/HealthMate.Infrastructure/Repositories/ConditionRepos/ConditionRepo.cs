using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Conditions.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.ConditionRepos{
    public class ConditionRepo : GenericRepository<Condition>, IConditionRepo{
        private readonly HealthMateContext _context;
        private readonly IDbContextFactory<HealthMateContext> _contextFactory;
        public ConditionRepo(IDbContextFactory<HealthMateContext> contextFactory,HealthMateContext context) : base(context)
        {
            _context = context;
            _contextFactory = contextFactory;
        }

        public async Task<PatientDashboardConditionReadDto> getMostRecentSevereOngoingCondition(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var conditions = await context.Conditions
                .Include(c => c.Disease)
                .Where(c => c.PaientId == patientId)
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.DateRecorded)
                .ToListAsync();

            if (!conditions.Any())
            {
                return new PatientDashboardConditionReadDto
                {
                    ConditionCode = "NO DATA",
                    ConditionName = "NO DATA",
                    ConditionDate = "NO DATA",
                    Treatement = "NO DATA",
                    IsOngoing = false
                };
            }

            var ongoingCondition = conditions.FirstOrDefault(c => c.isOngoing);
            var mostSevereCondition = conditions.First();

            var selectedCondition = ongoingCondition ?? mostSevereCondition;

            return new PatientDashboardConditionReadDto
            {
                ConditionCode = selectedCondition.Disease.Code,
                ConditionName = selectedCondition.Disease.Display_Name,
                ConditionDate = selectedCondition.DateRecorded.ToString("yyyy-MM-dd"),
                Treatement = selectedCondition.Note ?? "No treatment specified",
                IsOngoing = selectedCondition.isOngoing
            };
        }
    }
}