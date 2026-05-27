namespace HealthMate.Application.Prescriptions.Contracts{
    public class PrescriptionSummaryReadDto{
        public int PrescriptionId { get; set; }
        public string PrescriptionDate { get; set; } = null!;
        public string Publisher { get; set; } = null!;
        public string ConditionName { get; set; } = null!;
    }
}