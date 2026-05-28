using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Domain.Aggregates.Condition;
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
            
            var conditions = await (
                from condition in context.Conditions.AsNoTracking()
                join disease in context.Diseases.AsNoTracking() on condition.DiseaseId equals disease.Disease_Id
                where condition.PatientId == patientId
                orderby condition.Severity descending, condition.DateRecorded descending
                select new
                {
                    disease.Code,
                    disease.Display_Name,
                    condition.DateRecorded,
                    condition.Note,
                    IsOngoing = EF.Property<bool>(condition, "IsOngoing")
                })
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

            var ongoingCondition = conditions.FirstOrDefault(c => c.IsOngoing);
            var mostSevereCondition = conditions.First();

            var selectedCondition = ongoingCondition ?? mostSevereCondition;

            return new PatientDashboardConditionReadDto
            {
                ConditionCode = selectedCondition.Code,
                ConditionName = selectedCondition.Display_Name,
                ConditionDate = selectedCondition.DateRecorded.ToString("yyyy-MM-dd"),
                Treatement = selectedCondition.Note ?? "No treatment specified",
                IsOngoing = selectedCondition.IsOngoing
            };
        }
    }
}
