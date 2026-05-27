using HealthMate.Application.Prescriptions.Contracts.Medicines;

namespace HealthMate.Application.Prescriptions.Contracts{
    public class PrescriptionDetailsReadDto{
        public string PatientName { get; set; } = null!;
        public string PatientNationalId { get; set; } = null!;
        public string PrescriptionDate { get; set; } = null!;
        public string DiseaseName { get; set; } = null!;
        public List<MedicineDetailsReadDto> Medicines { get; set; } = new();
    }
}