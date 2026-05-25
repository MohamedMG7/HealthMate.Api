using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO.ConditionDto;

namespace HealthMate.Infrastructure.Repositories.ConditionRepos{
    public interface IConditionRepo : IGenericRepository<Condition>{
        Task<PatientDashboardConditionReadDto> getMostRecentSevereOngoingCondition(int patinetId);
    }
}