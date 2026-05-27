namespace HealthMate.Application.Identity.Contracts{
	public class RegistrationResult
	{
		public AccountOperationResult Result { get; set; } = null!;
		public string UserId { get; set; } = null!;
	}

    public class AccountOperationResult
    {
        public bool Succeeded { get; set; }
        public IEnumerable<AccountFailure> Errors { get; set; } = [];

        public static AccountOperationResult Success() => new() { Succeeded = true };

        public static AccountOperationResult Failed(params AccountFailure[] errors) => new() { Errors = errors };
    }

	public class RegistrationValidationError{
		public string Code { get; set; } = null!;
		public string Description { get; set; } = null!;
	}
}
