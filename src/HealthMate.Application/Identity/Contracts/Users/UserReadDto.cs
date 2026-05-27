using HealthMate.Application.Abstractions.Enums;


namespace HealthMate.Application.Identity.Contracts.Users{
	public class UserReadDto
	{
		public string Id { get; set; } // Provided by Identity
		public string Email { get; set; } // Provided by Identity
		public bool EmailConfirmed { get; set; }
		public string First_Name { get; set; }
		public string Last_Name { get; set; }
		public UserType UserType { get; set; }
		public bool IsActive { get; set; }
		public string? ImageUrl { get; set; }
	}
}
