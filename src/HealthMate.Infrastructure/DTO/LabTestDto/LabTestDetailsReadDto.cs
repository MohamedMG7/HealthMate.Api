namespace HealthMate.Infrastructure.DTO.LabTestDto{
    public class LabTestDetailsReadDto{
        public string PatientName { get; set; } = null!;
        public string PatientNationalId { get; set; } = null!;
        public string LabTestName { get; set; } = null!;
        public string LabTestDate { get; set; } = null!;
        public string? LabTestImageUrl { get; set; }
        public List<LabTestResultDto> Results { get; set; } = null!;
    }
}