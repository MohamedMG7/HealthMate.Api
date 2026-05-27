using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Prescriptions.Contracts;

namespace HealthMate.Application.Encounters.Contracts{
    public class EncounterSessionDataDto
    {
        // Encounter Metadata
        public int EncounterId { get; set; }
        public string PatientNationalId { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string HealthCareProviderName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReasonToVisit { get; set; } = null!;
        public string TreatmentPlan { get; set; } = null!;
        public string? Note { get; set; }

        // Encounter Data
        public List<ConditionReadDto> Conditions { get; set; } = new();
        public List<PrescriptionDetailsReadDto> Prescriptions { get; set; } = new();
    }
} 