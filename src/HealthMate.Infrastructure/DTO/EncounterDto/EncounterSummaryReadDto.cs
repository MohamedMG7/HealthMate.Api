namespace HealthMate.Infrastructure.DTO.EncounterDto{
    public class EncounterSumaryReadDto{
        public int EncounterId { get; set; }
        public string ConditionName { get; set; } = null!;
        public string EncounterDate { get; set; } = null!;
    }

}