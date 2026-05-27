using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Prescriptions.Contracts.Medicines;
using HealthMate.Application.Prescriptions.Contracts;

namespace HealthMate.Application.Encounters.Contracts{
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
