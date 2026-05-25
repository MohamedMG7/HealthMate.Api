namespace HealthMate.Infrastructure.DTO.MedicalImageDto{
    public class MedicalImageDetailsReadDto{
        public string PatientNationalId { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string MedicalImageName { get; set; } = null!;
        public string? Date { get; set; }
        public string? imageUrl { get; set; }
        public string Interpretation { get; set; } = null!;
    }
}