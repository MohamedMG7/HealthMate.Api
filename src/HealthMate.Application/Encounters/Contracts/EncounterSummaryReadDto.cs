namespace HealthMate.Application.Encounters.Contracts{
    public class EncounterSumaryReadDto{
        public int EncounterId { get; set; }
        public string ConditionName { get; set; } = null!;
        public string EncounterDate { get; set; } = null!;
    }

}