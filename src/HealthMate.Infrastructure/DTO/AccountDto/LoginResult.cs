using Microsoft.AspNetCore.Identity;

namespace HealthMate.Infrastructure.DTO.AccountDto
{
	public class LoginResult
	{
		public bool Succeeded { get; set; } 
		public string Token { get; set; } = null!; 
		public IEnumerable<IdentityError> Errors { get; set; } = null!; 
	
		public static LoginResult Success(string token)
		{
			return new LoginResult
			{
				Succeeded = true,
				Token = token,
				Errors = null!
			};
		}

		
		public static LoginResult Failed(params IdentityError[] errors)
		{
			return new LoginResult
			{
				Succeeded = false,
				Token = null,
				Errors = errors
			};
		}
	}
}
