using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Repositories{
    public interface ILabTestAttributeRepo : IGenericRepository<LabTestAttribute>{
        Task<int> GetIdByNameAsync(string AttributeName);
    }
}