using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Infrastructure.Data.Models
{
	public class VerificationCode
	{
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public string ApplicationUser_Id { get; set; } = null!;
        public string VerificationCodeDigits { get; set; } = null!;
        public DateTime ExpirationDate { get; set; }
        public VerificationPurpose Purpose { get; set; }
    }
}
