using HealthMate.Infrastructure.DTO.DiseaseDto;

namespace HealthMate.Application.Manager.DiseaseManager{
    public interface IDiseaseManager{
        Task<List<DiseaseNameAndIdDto>> getDiseasesNameAndId(); 
    }
}