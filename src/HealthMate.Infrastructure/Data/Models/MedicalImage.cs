using HealthMate.Domain.Aggregates.Patient;

namespace HealthMate.Infrastructure.Data.Models{
    public class MedicalImage{
        public int MedicalImageId { get; set; }
        public Patient patient { get; set; } = null!;
        public int paitentId { get; set; }
        public string MedicalImageName { get; set; } = null!;
        public string? MedicalImageUrl { get; set; } = null!;
        public string Interpertation { get; set; } = null!;
        public DateTime TimeRecorded { get; set; }
    }
}
