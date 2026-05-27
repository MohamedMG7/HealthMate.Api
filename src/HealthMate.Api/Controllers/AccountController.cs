using HealthMate.Application.Identity.Contracts;
using HealthMate.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;



namespace HealthMate.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
        private readonly IAccountManager _AccountManager;
        public AccountController(IAccountManager AccountManager)
        {
            _AccountManager = AccountManager;
        }

        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegistrationResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(RegistrationValidationError))]
		public async Task<IActionResult> Register([FromForm] AccountRegisterDto registerDto)
		{	

			#region Validation
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			// profile Picture validations
			if(registerDto.Image.Length > 2 * 1024 * 1024){
				return StatusCode(StatusCodes.Status400BadRequest, new RegistrationValidationError{
					Code = "FileSizeExceeded",
					Description = "File size should not exceed 2 MB"
				});
			}

			string[] allowedExtensions = {".jpg",".png",".jpeg"};
			var fileExtension = Path.GetExtension(registerDto.Image.FileName);
			if(!allowedExtensions.Contains(fileExtension)){
				return StatusCode(StatusCodes.Status400BadRequest,new RegistrationValidationError{
					Code = "NotAllowedExtension",
					Description = "This File Extension Is not Allowed"
				});
			}

			#endregion
			

			var result = await _AccountManager.RegisterUser(registerDto);
			if (result.Result.Succeeded)
			{
				return StatusCode(StatusCodes.Status201Created,result);
			}

			// Return the errors if the registration failed
			foreach (var error in result.Result.Errors)
			{
				return StatusCode(StatusCodes.Status400BadRequest,new RegistrationValidationError{
					Code = error.Code,
					Description = error.Description
				});
			}

			return BadRequest(ModelState);
		}

		// create confirmemail 
		[HttpPost("ConfirmEmail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
		public async Task<IActionResult> ConfirmEmail(EmailConfirmationDto emailConfirmationDto) {
			// Ensure the DTO is valid
			if (emailConfirmationDto == null || string.IsNullOrEmpty(emailConfirmationDto.Email) || string.IsNullOrEmpty(emailConfirmationDto.VerificationCode))
			{
				return BadRequest(new { Message = "Invalid email or verification code." });
			}

			// Call the AccountManager to confirm the email
			var result = await _AccountManager.ConfirmEmailAddress(emailConfirmationDto);

			// Handle the result
			if (result.StartsWith("Email Confirmed"))
			{
				return Ok(new { Message = "Email Confirmed Successfully" });
			}

			// Handle unsuccessful confirmation
			return BadRequest(new { Message = result });
		}

		[HttpPost("Login")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
		public async Task<IActionResult> Login([FromBody] AccountLoginDto accountLoginDto) { 
			var result = await _AccountManager.LoginUser(accountLoginDto);
			if (result.Succeeded)
			{
				return Ok(new { JwtToken = result.Token});
			}
			else {
				return NotFound(result);
			}
		}

		[HttpPost("ResetPassword")]
		[Authorize(policy:"PatientOrHealthCareProvider")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordData)
		{
			// Validate the input
			if (resetPasswordData == null || string.IsNullOrEmpty(resetPasswordData.ApplicationUserId) || string.IsNullOrEmpty(resetPasswordData.CurrentPassword) || string.IsNullOrEmpty(resetPasswordData.NewPassword))
			{
				return BadRequest(new { Message = "Invalid input data." });
			}

			// Call the AccountManager to reset the password
			var result = await _AccountManager.ResetPassword(resetPasswordData);

			// Handle the result
			if (result == "Password Changed Correctly")
			{
				return Ok(new { Message = result });
			}

			// Handle unsuccessful password reset
			return BadRequest(new { Message = result });
		}

		[HttpPost("send-verification-code")]
		public async Task<IActionResult> SendVerificationCodeForgotPassword([FromQuery]string emailAddress)
		{
			var result = await _AccountManager.SendForgotPasswordVerificationCodeAsync(emailAddress);
			if (result.StartsWith("Verification code sent"))
				return Ok(result);
			
			return BadRequest(result);
		}

		// Endpoint to verify code and reset password
		[HttpPost("forgot-password")]
		public async Task<IActionResult> ChangeForgotPasswordAsync([FromBody]ForgotPasswordDto Data)
		{
			var result = await _AccountManager.ChangeForgotPasswordAsync(Data);
			if (result == "Password reset successful.")
				return Ok(result);
			
			return BadRequest(result);
		}

		[HttpPost("verify-code")]
		public async Task<IActionResult> VerifyCode(VerifyCodeDto Data){
			var result = await _AccountManager.verifyForgotPasswordCode(Data);

			if(result){
				return Ok();
			}

			return BadRequest();
		}
	}
}
