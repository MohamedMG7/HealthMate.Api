namespace HealthMate.Infrastructure.Data.Models{
    public class LabTestResult{
        public int Id { get; set; }
        public int LabTestId { get; set; }
        public int LabTestAttributeId { get; set; }
        public decimal Value { get; set; }

        public LabTest LabTest { get; set; } = null!;
        public LabTestAttribute LabTestAttribute { get; set; } = null!;
    }
}