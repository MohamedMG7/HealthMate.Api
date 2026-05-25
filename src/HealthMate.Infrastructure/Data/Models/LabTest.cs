using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models{
    
    public class LabTest{

        public int LabTestId { get; set; }
        public Patient patient { get; set; } = null!;
        public int patientId { get; set; }
        public string LabTestName { get; set; } = null!;
        public DateTime RecordedTime { get; set; } 
        public string? ImageUrl { get; set; } = null!;
        public string? Note { get; set; }
        public string NameIdentifier { get; set; } = null!; // this is used as identifier to sina
        public ICollection<LabTestResult>? LabTestResults { get; set; } = new HashSet<LabTestResult>();
    }

}