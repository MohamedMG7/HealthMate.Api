using Microsoft.AspNetCore.Identity;

namespace HealthMate.Infrastructure.DTO.AccountDto
{
	public class RegistrationResult
	{
		public IdentityResult Result { get; set; } = null!;
		public string UserId { get; set; } = null!;
	}

	public class RegistrationValidationError{
		public string Code { get; set; } = null!;
		public string Description { get; set; } = null!;
	}
}
