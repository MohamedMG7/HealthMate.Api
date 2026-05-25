using System.Security.Principal;

namespace HealthMate.Infrastructure.DTO.AdminDto
{
	public class AdminApprovalDto
	{
        public int PatientId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
