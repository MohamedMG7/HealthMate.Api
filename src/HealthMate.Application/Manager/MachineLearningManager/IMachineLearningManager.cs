using HealthMate.Application.Ml.Contracts;

namespace HealthMate.Application.Manager.MachineLearningManager;

public interface IMachineLearningManager
{
    Task<MachineLearningResponse> CheckAnimea(int patientId, CancellationToken cancellationToken = default);
}
