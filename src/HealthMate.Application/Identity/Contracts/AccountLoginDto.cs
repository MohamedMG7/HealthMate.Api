
namespace HealthMate.Application.Identity.Contracts{
	public class AccountLoginDto
	{
        // can sign in using national ID number or email and password / not now
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool StayLoggedIn { get; set; }
        //public string? NationalId { get; set; }
    }
}
