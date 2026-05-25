using HealthMate.Infrastructure.DTO.LabTestDto;
using HealthMate.Infrastructure.DTO.ObservationDto;
using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.EndEcnounterDto;
using EndEncounterDto;



namespace HealthMate.Infrastructure.DTO.HealthCareProviderDto{
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