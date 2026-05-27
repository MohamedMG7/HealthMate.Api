using HealthMate.Application.LabTests.Contracts;

namespace HealthMate.Application.Manager.LabTestManager{
    public interface ILabTestManager{
        Task addLabTestAsync(LabTestAddDto LabTest);
    }
}