/// <summary>
/// Manages user account-related operations such as login, logout, and registration. 
/// Provides functionalities for user authentication and token generation.
/// </summary>
/// <remarks>
/// This class interacts with the underlying data repository for account management, 
/// including user registration and retrieval, and works with ASP.NET Core Identity to handle user authentication. 
/// It also generates JSON Web Tokens (JWT) for authenticated users.
/// 
/// Dependencies:
/// - `IGenericRepository<ApplicationUser>`: A generic repository for interacting with user data.
/// - `UserManager<ApplicationUser>`: ASP.NET Core Identity manager for handling user-related tasks.
/// - `IConfiguration`: Provides access to application settings, including JWT configuration.
/// 
/// Key Methods:
/// - `LoginUser`: Logs in a user based on their credentials (to be implemented).
/// - `LogoutUser`: Logs out the currently authenticated user (to be implemented).
/// - `RegisterUser`: Registers a new user with the provided details (to be implemented).
/// - `GenerateToken`: Generates a JWT for a given set of claims and remembers the user session based on a specified duration.
/// </remarks>


using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.DTO.AccountDto;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using HealthMate.Infrastructure.Enums;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Diagnostics.Eventing.Reader;

namespace HealthMate.Application.Identity
{
	public class AccountManager : IAccountManager
	{
		private readonly IGenericRepository<HealthCareProvider> _HealthCareProviderRepo;
		private readonly IGenericRepository<Admin> _AdminRepo;
		private readonly IGenericRepository<Patient> _PatientRepo;
		private readonly IVerificationCodeStore _verificationCodes;
		private readonly UserManager<ApplicationUser> _UserManager;	
		private readonly RoleManager<IdentityRole> _RoleManager;
		private readonly IConfiguration _Configuration;
		private readonly IEmailService _EmailService;
		private readonly IFileService _FileService;
        public AccountManager(UserManager<ApplicationUser> UserManager, IConfiguration Configuration, RoleManager<IdentityRole> RoleManager, IVerificationCodeStore verificationCodes, IEmailService emailService, IGenericRepository<Patient> patientRepo, IGenericRepository<Admin> AdminRepo, IGenericRepository<HealthCareProvider> HealthCareProviderRepo, IFileService FileService)
        {
			_UserManager = UserManager;
			_Configuration = Configuration;
			_RoleManager = RoleManager;	
			_verificationCodes = verificationCodes;
			_EmailService = emailService;
			_PatientRepo = patientRepo;
			_AdminRepo = AdminRepo;
			_HealthCareProviderRepo = HealthCareProviderRepo;
			_FileService = FileService;
        }
        public async Task<LoginResult> LoginUser(AccountLoginDto loginDto)
		{
			// who is logging in?
			var user = await _UserManager.FindByEmailAsync(loginDto.Email!);
			
			// Check if user exists
			if (user == null)
			{
				return LoginResult.Failed(new IdentityError
				{
					Code = "UserNotFound",
					Description = "User not found"
				});
			}

			var userRoles = await _UserManager.GetRolesAsync(user);
			string userRole = userRoles.FirstOrDefault()!;

			//check password
			bool isPasswordValid = await _UserManager.CheckPasswordAsync(user, loginDto.Password);
			if (!isPasswordValid)
			{
				return LoginResult.Failed(new IdentityError
				{
					Code = "InvalidPassword",
					Description = "Invalid password"
				});
			}

			// Add Claims and generate token
			List<Claim> claims = new List<Claim>()
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, user.Email!),
				new Claim(ClaimTypes.Role, userRole),
				new Claim("UserName", user.FullName ?? string.Empty)
			};

			// Handle different user roles
			if (userRole == "Patient")
			{
				var patient = _PatientRepo.GetAll().FirstOrDefault(p => p.ApplicationUserId == user.Id);
				if (patient == null)
				{
					return LoginResult.Failed(new IdentityError
					{
						Code = "NoPatientRecord",
						Description = "No Paitent record found"
					});
				}
				claims.Add(new Claim("PatientId",patient.Patient_Id.ToString()));
			}
			else if (userRole == "HealthCareProvider")
			{
				var healthCareProvider = _HealthCareProviderRepo.GetAll().FirstOrDefault(p => p.ApplicationUserId == user.Id);
				if (healthCareProvider == null)
				{
					return LoginResult.Failed(new IdentityError
					{
						Code = "NoProviderRecord",
						Description = "No healthcare provider record found"
					});
				}
				claims.Add(new Claim("HealthCareProviderId",healthCareProvider.HealthCareProvider_Id.ToString()));
			}
			else if (userRole == "Admin")
			{
				var admin = _AdminRepo.GetAll().FirstOrDefault(p => p.ApplicationUserId == user.Id);
				if (admin == null)
				{
					return LoginResult.Failed(new IdentityError
					{
						Code = "NoAdminRecord",
						Description = "No admin record found"
					});
				}
			}

			string Token = GenerateToken(claims, loginDto.StayLoggedIn);
			return LoginResult.Success(Token);
		}

		public Task LogoutUser()
		{
			throw new NotImplementedException();
		}

		public async Task<RegistrationResult> RegisterUser(AccountRegisterDto registerDto)
		{
			#region Validation
			if (!Enum.IsDefined(typeof(UserType), registerDto.UserType))
			{
				return new RegistrationResult
				{
					Result = IdentityResult.Failed(new IdentityError
					{
						Code = "InvalidUserType",
						Description = "The provided UserType is invalid."
					})
				};
			}

			if (_UserManager.Users.Any(s => s.Email == registerDto.Email)) {
				return new RegistrationResult
				{
					Result = IdentityResult.Failed(new IdentityError
					{
						Code = "EmailAlreadyExists",
						Description = "The Provided Email Is Already Used"
					})
				};
			}
			#endregion

			

			#region Register User
			var user = new ApplicationUser
			{
				UserName = registerDto.Email,
				First_Name = registerDto.First_Name,
				Last_Name = registerDto.Last_Name,
				Email = registerDto.Email,
				UserType = registerDto.UserType,
				PhoneNumber = registerDto.PhoneNumber,
				IsActive = false
			};
			
			var result = await _UserManager.CreateAsync(user,registerDto.Password);
			#endregion

			#region Upload Profile Picture
			
			user.ImageUrl = await _FileService.SaveFileAsync(registerDto.Image,"Profile_Pictures",user.Id);
			#endregion

			#region Assign Role
			string roleName = registerDto.UserType.ToString();

			if (result.Succeeded) {
				if (await _RoleManager.RoleExistsAsync(roleName)) {
					var roleResult = await _UserManager.AddToRoleAsync(user,roleName);
					if (!roleResult.Succeeded)
					{
						return new RegistrationResult
						{
							Result = IdentityResult.Failed(new IdentityError
							{
								Code = "RoleAssignmentFailed",
								Description = "Failed to assign role to the user."
							})
						};
					}
					
					// Generate Verification Code
					string confirmationCode = GenerateVerificationCode();
					// Add Verification code to DB
					_verificationCodes.AddCode(user.Id, confirmationCode, DateTime.UtcNow.AddMinutes(10), VerificationCodePurpose.EmailConfirmation);

					//compose email body and send it
					string confirmationEmailBody = $"Dear {user.First_Name} \n\n  here is your verification code: {confirmationCode} \n it expires in 10 minutes";
					var res = await _EmailService.SendEmailAsync(user.Email, "Confirm Your Email Address", confirmationEmailBody);

					_verificationCodes.Save();
					return new RegistrationResult
					{
						Result = IdentityResult.Success,
						UserId = user.Id
					};
				}
			}
			#endregion
			return new RegistrationResult { Result = result, UserId = user.Id};
		}

		private string GenerateToken(IList<Claim> claims, bool RememberMe)
		{
			var SecretKeyString = _Configuration["Jwt:Key"];
			var issuer = _Configuration["Jwt:Issuer"];
			var audience = _Configuration["Jwt:Audience"];
			var SecretKeyByte = Encoding.ASCII.GetBytes(SecretKeyString!);
			SecurityKey securityKey = new SymmetricSecurityKey(SecretKeyByte);

			//Combind SecretKey , HasingAlgorithm (SigningCredentials)
			SigningCredentials signingCredential = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
			DateTime tokenExpiration = RememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(2);
			JwtSecurityToken jwtSecurityToken = new JwtSecurityToken
			(
				claims: claims,
				issuer: issuer,
				audience: audience,
				signingCredentials: signingCredential,
				expires: tokenExpiration
			);

			JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
			string token = handler.WriteToken(jwtSecurityToken);
			return token;
		}

		private string GenerateVerificationCode() { 
			var rand = new Random();

			string verificationCode = rand.Next(1000, 9999).ToString();

			return verificationCode;
		}

		public async Task<string> ConfirmEmailAddress(EmailConfirmationDto emailConfirmationDto) {
			// Validate input
			if (emailConfirmationDto == null || string.IsNullOrEmpty(emailConfirmationDto.Email) || string.IsNullOrEmpty(emailConfirmationDto.VerificationCode))
			{
				return "Invalid email or confirmation code.";
			}

			// Retrieve the user by email
			var user = await _UserManager.FindByEmailAsync(emailConfirmationDto.Email);
			if (user == null)
			{
				return "User not found.";
			}

			// Check if the verification code matches
			bool verificationEntry = _verificationCodes.IsValid(user.Id, emailConfirmationDto.VerificationCode, VerificationCodePurpose.EmailConfirmation);
			if (!verificationEntry)
			{
				return "Invalid verification code.";
			}

			// Mark the email as confirmed
			user.EmailConfirmed = true;

			// Save the changes to the database
			var updateResult = await _UserManager.UpdateAsync(user);
			if (updateResult.Succeeded)
			{
				_verificationCodes.Delete(user.Id, emailConfirmationDto.VerificationCode);
				_verificationCodes.Save();
				return "Email Confirmed";
			}

			return "Failed to update user email confirmation.";
		}
		public async Task<string> ResetPassword(ResetPasswordDto ResetPasswordData){
    
			var user = await _UserManager.FindByIdAsync(ResetPasswordData.ApplicationUserId);
			
			if(user == null){
				return "Email Is Wrong";
			}

			if(!await _UserManager.CheckPasswordAsync(user,ResetPasswordData.CurrentPassword)){
				return "Wrong Passsword";
			}

			if(ResetPasswordData.CurrentPassword == ResetPasswordData.NewPassword){
				return "The New Password Can not Be the Same As Current Password";
			}

			await _UserManager.ChangePasswordAsync(user,ResetPasswordData.CurrentPassword,ResetPasswordData.NewPassword);
			return "Password Changed Correctly";
		}

		public async Task<string> SendForgotPasswordVerificationCodeAsync(string emailAddress)
		{
			var user = await _UserManager.FindByEmailAsync(emailAddress);
			if (user == null)
			{
				return "User not found.";
			}

			var random = new Random();
			var verificationCode = random.Next(1000, 9999).ToString();

			if (!_verificationCodes.DeleteAllForUser(user.Id))
			{
				return "something is wrong";
			}

			_verificationCodes.AddCode(user.Id, verificationCode, DateTime.UtcNow.AddMinutes(10), VerificationCodePurpose.ForgotPassword);
			_verificationCodes.Save();

			var emailSubject = "Password Reset Verification Code";
			var emailMessage = $"Your verification code is: {verificationCode}";
			var emailResult = await _EmailService.SendEmailAsync(emailAddress, emailSubject, emailMessage);

			return emailResult == "Email Sent" 
				? "Verification code sent successfully. Please check your email." 
				: "Failed to send verification email.";
		}
		
		public async Task<string> ChangeForgotPasswordAsync(ForgotPasswordDto Data)
		{
			var user = await _UserManager.FindByEmailAsync(Data.emailAddress);
			if (user == null)
			{
				return "User not found.";
			}

			var resetToken = await _UserManager.GeneratePasswordResetTokenAsync(user);
			var resetResult = await _UserManager.ResetPasswordAsync(user, resetToken, Data.NewPassword);

			if (!resetResult.Succeeded)
			{
				return "Password reset failed: " + string.Join(", ", resetResult.Errors.Select(e => e.Description));
			}

			return "Password reset successful.";
		}

		public async Task<bool> verifyForgotPasswordCode(VerifyCodeDto Data){
			var user = await _UserManager.FindByEmailAsync(Data.Email);
			if (user == null)
			{
				return false;
			}

			bool IsVerified = _verificationCodes.IsValid(user.Id, Data.Code, VerificationCodePurpose.ForgotPassword);

			if(!IsVerified){
				return false;
			} 

			return true;

		}


	}
}
