namespace HealthMate.Application.Prescriptions.Contracts.Medicines{
    public class MedicineDetailsReadDto{
        public string Name { get; set; } = null!;
        public int FrequencyInHours { get; set; } 
        public int DurationInDays   { get; set; } 
        public string Dose { get; set; } = null!;
    }
}