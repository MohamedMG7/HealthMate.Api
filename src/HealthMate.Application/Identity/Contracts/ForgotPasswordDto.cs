namespace HealthMate.Application.Identity.Contracts{
    public class ForgotPasswordDto{
        public string emailAddress { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}