using HealthMate.Application.Prescriptions.Contracts.Medicines;

namespace HealthMate.Application.Prescriptions.Contracts.Medicines{
    public interface IMedicineRepo{
        Task<List<MedicineNameAndIdDto>> getMedicineNameAndId();
    }
}
