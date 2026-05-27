using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Conditions.Contracts;

namespace HealthMate.Infrastructure.Repositories.ConditionRepos{
    public interface IConditionRepo : IGenericRepository<Condition>{
        Task<PatientDashboardConditionReadDto> getMostRecentSevereOngoingCondition(int patinetId);
    }
}