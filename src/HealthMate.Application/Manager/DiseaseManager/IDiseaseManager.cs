using HealthMate.Application.Clinical.Contracts;

namespace HealthMate.Application.Manager.DiseaseManager{
    public interface IDiseaseManager{
        Task<List<DiseaseNameAndIdDto>> getDiseasesNameAndId(); 
    }
}