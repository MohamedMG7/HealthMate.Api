namespace HealthMate.Infrastructure.DTO.AccountDto{
    public class ResetPasswordDto{
        public string ApplicationUserId { get; set; } = null!;
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}