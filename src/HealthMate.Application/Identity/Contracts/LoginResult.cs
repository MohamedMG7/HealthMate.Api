namespace HealthMate.Application.Identity.Contracts{
	public class LoginResult
	{
		public bool Succeeded { get; set; } 
		public string Token { get; set; } = null!; 
		public IEnumerable<AccountFailure> Errors { get; set; } = [];
	
		public static LoginResult Success(string token)
		{
			return new LoginResult
			{
				Succeeded = true,
				Token = token,
				Errors = []
			};
		}

		
		public static LoginResult Failed(params AccountFailure[] errors)
		{
			return new LoginResult
			{
				Succeeded = false,
				Token = null,
				Errors = errors
			};
		}
	}

    public sealed record AccountFailure(string Code, string Description);
}
