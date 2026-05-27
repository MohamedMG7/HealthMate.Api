namespace HealthMate.Application.Encounters.Contracts{
    public class patientDashboardEncounterHistory{
        public int EncounterId { get; set; }
        public string HcpName { get; set; } = null!;
        public string HcpSpecialization { get; set; } = null!;
        public string EncounterDate { get; set; } = null!;
    }
}