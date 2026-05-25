namespace HealthMate.Infrastructure.DTO.MedicalImageDto{
    public class MedicalImageSummaryReadDto{
        public int MedicalImageId { get; set; }
        public string MedicalImageName { get; set; } = null!;
        public string MedicalImageDate { get; set; } = null!;
    }
}