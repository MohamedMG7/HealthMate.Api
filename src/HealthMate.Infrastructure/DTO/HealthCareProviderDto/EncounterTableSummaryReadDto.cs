namespace HealthMate.Infrastructure.DTO.HealthCareProviderDto
{
	public class EncounterTableSummaryReadDto
	{
        public int EncounterId { get; set; }
        public string Patient_Name { get; set; } = null!;
        public string Patient_Id { get; set; } = null!;
        public DateOnly EncounterDate { get; set; }
        public string Diagnosis { get; set; } = null!;
    }
}
