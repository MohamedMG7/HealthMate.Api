using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace HealthMate.Infrastructure.Identity.Adapters;

public sealed class AspNetIdentityAccountGateway(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IIdentityAccountGateway
{
    public async Task<AccountSummary?> FindByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<AccountSummary?> FindByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : Map(user);
    }

    public async Task<IdentityGatewayResult<AccountSummary>> CreateAsync(NewUserDescriptor user, string password)
    {
        var applicationUser = new ApplicationUser
        {
            UserName = user.Email,
            First_Name = user.FirstName,
            Last_Name = user.LastName,
            Email = user.Email,
            UserType = user.UserType,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive
        };

        var result = await userManager.CreateAsync(applicationUser, password);
        return result.Succeeded
            ? IdentityGatewayResult<AccountSummary>.Success(Map(applicationUser))
            : IdentityGatewayResult<AccountSummary>.Failed(MapErrors(result).ToArray());
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId)
    {
        var user = await RequireUserAsync(userId);
        return (await userManager.GetRolesAsync(user)).ToArray();
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityGatewayResult> AddToRoleAsync(string userId, string role)
    {
        var user = await RequireUserAsync(userId);
        return ToResult(await userManager.AddToRoleAsync(user, role));
    }

    public Task<bool> RoleExistsAsync(string role) => roleManager.RoleExistsAsync(role);

    public async Task<IdentityGatewayResult> SetEmailConfirmedAsync(string userId)
    {
        var user = await RequireUserAsync(userId);
        user.EmailConfirmed = true;
        return ToResult(await userManager.UpdateAsync(user));
    }

    public async Task<IdentityGatewayResult> SetProfileImageUrlAsync(string userId, string? imageUrl)
    {
        var user = await RequireUserAsync(userId);
        user.ImageUrl = imageUrl;
        return ToResult(await userManager.UpdateAsync(user));
    }

    public async Task<IdentityGatewayResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await RequireUserAsync(userId);
        return ToResult(await userManager.ChangePasswordAsync(user, currentPassword, newPassword));
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string userId)
    {
        var user = await RequireUserAsync(userId);
        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<IdentityGatewayResult> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        var user = await RequireUserAsync(userId);
        return ToResult(await userManager.ResetPasswordAsync(user, token, newPassword));
    }

    private async Task<ApplicationUser> RequireUserAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' was not found.");
    }

    private static AccountSummary Map(ApplicationUser user)
    {
        return new AccountSummary(
            user.Id,
            user.Email,
            user.EmailConfirmed,
            user.FullName,
            user.First_Name,
            user.Last_Name,
            user.IsActive,
            user.ImageUrl);
    }

    private static IdentityGatewayResult ToResult(IdentityResult result)
    {
        return result.Succeeded ? IdentityGatewayResult.Success() : IdentityGatewayResult.Failed(MapErrors(result).ToArray());
    }

    private static IEnumerable<IdentityGatewayError> MapErrors(IdentityResult result)
    {
        return result.Errors.Select(error => new IdentityGatewayError(error.Code, error.Description));
    }
}
