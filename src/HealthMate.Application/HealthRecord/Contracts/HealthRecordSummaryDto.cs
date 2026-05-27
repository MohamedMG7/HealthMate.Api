using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Prescriptions.Contracts.Medicines;
using HealthMate.Application.Prescriptions.Contracts;

namespace HealthMate.Application.HealthRecord.Contracts{
    public class HealthRecordSummaryDto{
        public ICollection<ConditionSummaryReadDto> ConditionsSummary { get; set; } = new HashSet<ConditionSummaryReadDto>();
        public ICollection<LabTestSummaryReadDto> LabTestsSummary { get; set; } = new HashSet<LabTestSummaryReadDto>();
        public ICollection<MedicalImageSummaryReadDto> MedicalImagesSummary { get; set; } = new HashSet<MedicalImageSummaryReadDto>();
        public ICollection<MedicineSummaryReadDto> MedicinesSummary { get; set; } = new HashSet<MedicineSummaryReadDto>();
        public ICollection<PrescriptionSummaryReadDto> PrescriptionsSummary { get; set; } = new HashSet<PrescriptionSummaryReadDto>();
        public ICollection<EncounterSumaryReadDto> EncountersSummary { get; set; } = new HashSet<EncounterSumaryReadDto>();
    }
}