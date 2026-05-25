namespace HealthMate.Infrastructure.Data.Models{
    public class LabTestAttribute{
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Abbreviation { get; set; } = null!;
        public string ValueUnit { get; set; } = null!;
        public string NormalRange { get; set; } = null!;

        public ICollection<LabTestResult> LabTestResults { get; set; } = new HashSet<LabTestResult>();
    }
}