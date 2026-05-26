using HealthMate.Infrastructure.DTO.AccountDto;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace HealthMate.Application.Identity
{
	public interface IAccountManager
	{
		Task<LoginResult> LoginUser(AccountLoginDto loginDto); // return token for sign in
		Task<RegistrationResult> RegisterUser(AccountRegisterDto registerDto); 
		Task LogoutUser();
		public Task<string> ConfirmEmailAddress(EmailConfirmationDto confirmEmailRequestDto);
		Task<string> ResetPassword(ResetPasswordDto ResetPasswordData);
		Task<string> ChangeForgotPasswordAsync(ForgotPasswordDto Data);
		Task<string> SendForgotPasswordVerificationCodeAsync(string emailAddress);
		Task<bool> verifyForgotPasswordCode(VerifyCodeDto Data);

	}
}
