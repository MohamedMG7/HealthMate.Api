using Microsoft.AspNetCore.Http;

namespace HealthMate.Infrastructure.DTO{
    public class PrescriptionWithMedicineAddDto{
        
        // Prescription metadata
        public string? Publisher { get; set; }
        public DateTime PrescriptionDate { get; set; }
        
        // List of patient medicines
        public ICollection<PatientMedicineAddDto> Medicines { get; set; } = new HashSet<PatientMedicineAddDto>();
    }
}