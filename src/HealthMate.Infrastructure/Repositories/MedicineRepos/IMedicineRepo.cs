using HealthMate.Infrastructure.DTO.MedicineDto;

namespace HealthMate.Infrastructure.Repositories{
    public interface IMedicineRepo{
        Task<List<MedicineNameAndIdDto>> getMedicineNameAndId();
    }
}