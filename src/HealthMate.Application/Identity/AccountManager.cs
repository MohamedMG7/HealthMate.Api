using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Application.Abstractions.Storage;
using HealthMate.Application.Identity.Contracts;

namespace HealthMate.Application.Identity;

public class AccountManager : IAccountManager
{
    private readonly IIdentityAccountGateway accounts;
    private readonly IJwtTokenIssuer tokenIssuer;
    private readonly IUserLinkedAccountLookup linkedAccounts;
    private readonly IVerificationCodeStore verificationCodes;
    private readonly IEmailService emailService;
    private readonly IFileStorage fileStorage;

    public AccountManager(
        IIdentityAccountGateway accounts,
        IJwtTokenIssuer tokenIssuer,
        IUserLinkedAccountLookup linkedAccounts,
        IVerificationCodeStore verificationCodes,
        IEmailService emailService,
        IFileStorage fileStorage)
    {
        this.accounts = accounts;
        this.tokenIssuer = tokenIssuer;
        this.linkedAccounts = linkedAccounts;
        this.verificationCodes = verificationCodes;
        this.emailService = emailService;
        this.fileStorage = fileStorage;
    }

    public async Task<LoginResult> LoginUser(AccountLoginDto loginDto)
    {
        var user = await accounts.FindByEmailAsync(loginDto.Email!);
        if (user is null)
        {
            return LoginResult.Failed(Failure("UserNotFound", "User not found"));
        }

        var userRoles = await accounts.GetRolesAsync(user.Id);
        var userRole = userRoles.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userRole))
        {
            return LoginResult.Failed(Failure("NoRole", "No role assigned to the user"));
        }

        if (!await accounts.CheckPasswordAsync(user.Id, loginDto.Password))
        {
            return LoginResult.Failed(Failure("InvalidPassword", "Invalid password"));
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, userRole),
            new("UserName", user.FullName)
        };

        if (userRole == nameof(UserType.Patient))
        {
            var patientId = await linkedAccounts.GetPatientIdByUserIdAsync(user.Id);
            if (patientId is null)
            {
                return LoginResult.Failed(Failure("NoPatientRecord", "No patient record found"));
            }

            claims.Add(new Claim("PatientId", patientId.Value.ToString()));
        }
        else if (userRole == nameof(UserType.HealthCareProvider))
        {
            var providerId = await linkedAccounts.GetHealthCareProviderIdByUserIdAsync(user.Id);
            if (providerId is null)
            {
                return LoginResult.Failed(Failure("NoProviderRecord", "No healthcare provider record found"));
            }

            claims.Add(new Claim("HealthCareProviderId", providerId.Value.ToString()));
        }
        else if (userRole == nameof(UserType.Admin))
        {
            var adminId = await linkedAccounts.GetAdminIdByUserIdAsync(user.Id);
            if (adminId is null)
            {
                return LoginResult.Failed(Failure("NoAdminRecord", "No admin record found"));
            }
        }

        return LoginResult.Success(tokenIssuer.Issue(claims, loginDto.StayLoggedIn));
    }

    public Task LogoutUser()
    {
        throw new NotImplementedException();
    }

    public async Task<RegistrationResult> RegisterUser(AccountRegisterDto registerDto)
    {
        if (!Enum.IsDefined(typeof(UserType), registerDto.UserType))
        {
            return RegistrationFailed("InvalidUserType", "The provided UserType is invalid.");
        }

        if (await accounts.FindByEmailAsync(registerDto.Email) is not null)
        {
            return RegistrationFailed("EmailAlreadyExists", "The Provided Email Is Already Used");
        }

        var createResult = await accounts.CreateAsync(
            new NewUserDescriptor(
                registerDto.Email,
                registerDto.First_Name,
                registerDto.Last_Name,
                registerDto.UserType,
                registerDto.PhoneNumber,
                IsActive: false),
            registerDto.Password);

        if (!createResult.Succeeded || createResult.Value is null)
        {
            return new RegistrationResult
            {
                Result = AccountOperationResult.Failed(createResult.Errors.Select(ToFailure).ToArray()),
                UserId = string.Empty
            };
        }

        var user = createResult.Value;
        var imageUrl = await SaveProfilePictureAsync(registerDto, user.Id);
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            var imageResult = await accounts.SetProfileImageUrlAsync(user.Id, imageUrl);
            if (!imageResult.Succeeded)
            {
                return new RegistrationResult
                {
                    Result = AccountOperationResult.Failed(imageResult.Errors.Select(ToFailure).ToArray()),
                    UserId = user.Id
                };
            }
        }

        var roleName = registerDto.UserType.ToString();
        if (!await accounts.RoleExistsAsync(roleName))
        {
            return RegistrationFailed("RoleNotFound", "The requested role does not exist.", user.Id);
        }

        var roleResult = await accounts.AddToRoleAsync(user.Id, roleName);
        if (!roleResult.Succeeded)
        {
            return new RegistrationResult
            {
                Result = AccountOperationResult.Failed(roleResult.Errors.Select(ToFailure).ToArray()),
                UserId = user.Id
            };
        }

        var confirmationCode = GenerateVerificationCode();
        verificationCodes.AddCode(user.Id, confirmationCode, DateTime.UtcNow.AddMinutes(10), VerificationCodePurpose.EmailConfirmation);

        var confirmationEmailBody = $"Dear {user.FirstName} \n\n  here is your verification code: {confirmationCode} \n it expires in 10 minutes";
        await emailService.SendEmailAsync(user.Email ?? registerDto.Email, "Confirm Your Email Address", confirmationEmailBody);

        verificationCodes.Save();
        return new RegistrationResult
        {
            Result = AccountOperationResult.Success(),
            UserId = user.Id
        };
    }

    public async Task<string> ConfirmEmailAddress(EmailConfirmationDto emailConfirmationDto)
    {
        if (emailConfirmationDto == null || string.IsNullOrEmpty(emailConfirmationDto.Email) || string.IsNullOrEmpty(emailConfirmationDto.VerificationCode))
        {
            return "Invalid email or confirmation code.";
        }

        var user = await accounts.FindByEmailAsync(emailConfirmationDto.Email);
        if (user is null)
        {
            return "User not found.";
        }

        if (!verificationCodes.IsValid(user.Id, emailConfirmationDto.VerificationCode, VerificationCodePurpose.EmailConfirmation))
        {
            return "Invalid verification code.";
        }

        var updateResult = await accounts.SetEmailConfirmedAsync(user.Id);
        if (updateResult.Succeeded)
        {
            verificationCodes.Delete(user.Id, emailConfirmationDto.VerificationCode);
            verificationCodes.Save();
            return "Email Confirmed";
        }

        return "Failed to update user email confirmation.";
    }

    public async Task<string> ResetPassword(ResetPasswordDto resetPasswordData)
    {
        var user = await accounts.FindByIdAsync(resetPasswordData.ApplicationUserId);
        if (user is null)
        {
            return "Email Is Wrong";
        }

        if (!await accounts.CheckPasswordAsync(user.Id, resetPasswordData.CurrentPassword))
        {
            return "Wrong Passsword";
        }

        if (resetPasswordData.CurrentPassword == resetPasswordData.NewPassword)
        {
            return "The New Password Can not Be the Same As Current Password";
        }

        await accounts.ChangePasswordAsync(user.Id, resetPasswordData.CurrentPassword, resetPasswordData.NewPassword);
        return "Password Changed Correctly";
    }

    public async Task<string> SendForgotPasswordVerificationCodeAsync(string emailAddress)
    {
        var user = await accounts.FindByEmailAsync(emailAddress);
        if (user is null)
        {
            return "User not found.";
        }

        var verificationCode = GenerateVerificationCode();
        if (!verificationCodes.DeleteAllForUser(user.Id))
        {
            return "something is wrong";
        }

        verificationCodes.AddCode(user.Id, verificationCode, DateTime.UtcNow.AddMinutes(10), VerificationCodePurpose.ForgotPassword);
        verificationCodes.Save();

        var emailResult = await emailService.SendEmailAsync(emailAddress, "Password Reset Verification Code", $"Your verification code is: {verificationCode}");
        return emailResult == "Email Sent"
            ? "Verification code sent successfully. Please check your email."
            : "Failed to send verification email.";
    }

    public async Task<string> ChangeForgotPasswordAsync(ForgotPasswordDto data)
    {
        var user = await accounts.FindByEmailAsync(data.emailAddress);
        if (user is null)
        {
            return "User not found.";
        }

        var resetToken = await accounts.GeneratePasswordResetTokenAsync(user.Id);
        var resetResult = await accounts.ResetPasswordAsync(user.Id, resetToken, data.NewPassword);
        if (!resetResult.Succeeded)
        {
            return "Password reset failed: " + string.Join(", ", resetResult.Errors.Select(e => e.Description));
        }

        return "Password reset successful.";
    }

    public async Task<bool> verifyForgotPasswordCode(VerifyCodeDto data)
    {
        var user = await accounts.FindByEmailAsync(data.Email);
        return user is not null && verificationCodes.IsValid(user.Id, data.Code, VerificationCodePurpose.ForgotPassword);
    }

    private async Task<string?> SaveProfilePictureAsync(AccountRegisterDto registerDto, string userId)
    {
        if (registerDto.Image is null || registerDto.Image.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(registerDto.Image.FileName);
        var fileName = string.IsNullOrWhiteSpace(extension) ? userId : $"{userId}{extension}";
        await using var stream = registerDto.Image.OpenReadStream();
        return await fileStorage.SaveAsync(stream, registerDto.Image.ContentType, "Profile_Pictures", fileName, CancellationToken.None);
    }

    private static string GenerateVerificationCode() => Random.Shared.Next(1000, 9999).ToString();

    private static AccountFailure Failure(string code, string description) => new(code, description);

    private static AccountFailure ToFailure(IdentityGatewayError error) => new(error.Code, error.Description);

    private static RegistrationResult RegistrationFailed(string code, string description, string userId = "")
    {
        return new RegistrationResult
        {
            Result = AccountOperationResult.Failed(Failure(code, description)),
            UserId = userId
        };
    }
}
