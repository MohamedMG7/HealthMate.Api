namespace HealthMate.Application.Identity.Contracts;

public class AdminApprovalDto
{
    public int PatientId { get; set; }
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}
