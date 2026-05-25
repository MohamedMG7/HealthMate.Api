namespace HealthMate.Infrastructure.DTO.MedicineDto{
    public class MedicineSummaryReadDto{
        public int patientMedicineId { get; set; }
        public string Name { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string DosePerTime { get; set; } = null!;
        public int DurationInDays { get; set; }
        public int FrequencyInHours { get; set; } 
        public bool isOngoing { get; set; }
    }
}