namespace HealthMate.Application.Identity.Contracts{
    public class VerifyCodeDto{
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}