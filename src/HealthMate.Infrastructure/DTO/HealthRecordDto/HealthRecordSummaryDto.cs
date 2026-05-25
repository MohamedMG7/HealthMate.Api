using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.LabTestDto;
using HealthMate.Infrastructure.DTO.MedicalImageDto;
using HealthMate.Infrastructure.DTO.MedicineDto;
using HealthMate.Infrastructure.DTO.PrescriptionDto;

namespace HealthMate.Infrastructure.DTO.HealthRecordDto{
    public class HealthRecordSummaryDto{
        public ICollection<ConditionSummaryReadDto> ConditionsSummary { get; set; } = new HashSet<ConditionSummaryReadDto>();
        public ICollection<LabTestSummaryReadDto> LabTestsSummary { get; set; } = new HashSet<LabTestSummaryReadDto>();
        public ICollection<MedicalImageSummaryReadDto> MedicalImagesSummary { get; set; } = new HashSet<MedicalImageSummaryReadDto>();
        public ICollection<MedicineSummaryReadDto> MedicinesSummary { get; set; } = new HashSet<MedicineSummaryReadDto>();
        public ICollection<PrescriptionSummaryReadDto> PrescriptionsSummary { get; set; } = new HashSet<PrescriptionSummaryReadDto>();
        public ICollection<EncounterSumaryReadDto> EncountersSummary { get; set; } = new HashSet<EncounterSumaryReadDto>();
    }
}