using HealthMate.Infrastructure.DTO.LabTestDto;

namespace HealthMate.Application.Manager.LabTestManager{
    public interface ILabTestManager{
        Task addLabTestAsync(LabTestAddDto LabTest);
    }
}