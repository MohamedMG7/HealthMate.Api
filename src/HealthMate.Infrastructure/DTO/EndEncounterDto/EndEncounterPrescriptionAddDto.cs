namespace HealthMate.Infrastructure.DTO{
    public class EndEncounterPrescriptionAddDto{
        
        // Prescription metadata
        //public string? Publisher { get; set; } this should be added automatically
        public DateTime PrescriptionDate { get; set; }
        
        // List of patient medicines
        public List<PatientMedicineAddDto> Medicines { get; set; } = new List<PatientMedicineAddDto>();
    }
}