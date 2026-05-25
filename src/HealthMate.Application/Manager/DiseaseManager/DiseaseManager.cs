using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO.DiseaseDto;
using HealthMate.Infrastructure.Repositories;

namespace HealthMate.Application.Manager.DiseaseManager{
    public class DiseaseManager : IDiseaseManager{
        private readonly IGenericRepository<Disease> _diseaseRepo;

        public DiseaseManager(IGenericRepository<Disease> diseaseRepo)
        {
            _diseaseRepo = diseaseRepo;
        }
        public async Task<List<DiseaseNameAndIdDto>> getDiseasesNameAndId(){
            var data = _diseaseRepo.GetAll()
            .Select(p => new DiseaseNameAndIdDto
            {
                Id = p.Disease_Id,
                Name = p.Display_Name
            })
            .ToList();

            return data;
        } 
    }
}