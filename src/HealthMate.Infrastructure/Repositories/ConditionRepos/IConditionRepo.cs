using HealthMate.Application.Conditions.Contracts;
using HealthMate.Domain.Aggregates.Condition;

namespace HealthMate.Infrastructure.Repositories.ConditionRepos{
    public interface IConditionRepo : IGenericRepository<Condition>{
        Task<PatientDashboardConditionReadDto> getMostRecentSevereOngoingCondition(int patinetId);
    }
}
