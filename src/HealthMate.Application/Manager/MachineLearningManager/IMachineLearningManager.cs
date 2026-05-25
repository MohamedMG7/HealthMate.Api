using HealthMate.Infrastructure.DTO.MachineLearningDto;

namespace HealthMate.Application.Manager.MachineLearningManager{
    public interface IMachineLearningManager{
        Task<MachineLearningResponse> CheckAnimea(int patientId);
        Task<MachineLearningResponse> CheckDiabetes(int patientId);

    }
}