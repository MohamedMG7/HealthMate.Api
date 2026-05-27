namespace HealthMate.Application.LabTests.Contracts{
    public class LabTestSummaryReadDto{
        public int LabTestId { get; set; }
        public string TestName { get; set; } = null!;
        public string TestDate { get; set; } = null!;
        public string Result { get; set; } = null!;
        public string Note { get; set; } = null!;
    }
}