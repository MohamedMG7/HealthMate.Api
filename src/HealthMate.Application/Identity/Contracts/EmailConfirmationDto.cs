namespace HealthMate.Application.Identity.Contracts{
	public class EmailConfirmationDto
	{
        public string Email { get; set; }
        public string VerificationCode { get; set; }
	}
}
