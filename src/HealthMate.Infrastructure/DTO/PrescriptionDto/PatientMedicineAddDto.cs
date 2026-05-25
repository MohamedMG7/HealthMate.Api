namespace HealthMate.Infrastructure.DTO
{
    public class PatientMedicineAddDto
    {
        public int MedicineId { get; set; }
        public int FrequencyInHours { get; set; }
        public int DurationInDays { get; set; }
        public bool IsPrescribed { get; set; }
        public string Dosage { get; set; } = null!;
    }
}