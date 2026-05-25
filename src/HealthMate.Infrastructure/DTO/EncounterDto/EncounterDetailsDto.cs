using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.LabTestDto;
using HealthMate.Infrastructure.DTO.MedicalImageDto;
using HealthMate.Infrastructure.DTO.MedicineDto;
using HealthMate.Infrastructure.DTO.PrescriptionDto;

namespace HealthMate.Infrastructure.DTO.EncounterDto
{
    public class EncounterDetailsDto
    {
        public string PatientNationalId { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string HealthCareProviderName { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string Reason_To_Visit { get; set; } = null!;
        public string Treatment_Plan { get; set; } = null!;
        public string Note { get; set; } = null!;
        public List<ConditionDetailsReadDto> Conditions { get; set; } = null!;
        public List<MedicineDetailsReadDto> Prescription { get; set; } = null!;
    }
}
