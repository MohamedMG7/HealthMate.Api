using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Documents.Contracts;



namespace HealthMate.Application.Encounters.Contracts{
    public class EndEncounter{
        //public int EncounterId { get; set; }
        public EndEncounterEncounterAddDto Encounter { get; set; } = null!;
        public EndEncounterPrescriptionAddDto? Prescription { get; set; }
        public ICollection<EndEncounterObservationAddDto>? Observations { get; set; }
        public EndEncounterConditionAddDto? Condition { get; set; }
        public EndEncounterLabTestAddDto? LabTests { get; set; }
        public ICollection<MedicalImageAddDto>? MedicalImages { get; set; }
    }
}
