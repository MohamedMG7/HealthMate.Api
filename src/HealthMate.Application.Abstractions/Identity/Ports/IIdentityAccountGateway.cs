using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.Abstractions.Identity.Ports;

public interface IIdentityAccountGateway
{
    Task<AccountSummary?> FindByEmailAsync(string email);
    Task<AccountSummary?> FindByIdAsync(string userId);
    Task<IdentityGatewayResult<AccountSummary>> CreateAsync(NewUserDescriptor user, string password);
    Task<IReadOnlyList<string>> GetRolesAsync(string userId);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<IdentityGatewayResult> AddToRoleAsync(string userId, string role);
    Task<bool> RoleExistsAsync(string role);
    Task<IdentityGatewayResult> SetEmailConfirmedAsync(string userId);
    Task<IdentityGatewayResult> SetProfileImageUrlAsync(string userId, string? imageUrl);
    Task<IdentityGatewayResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string userId);
    Task<IdentityGatewayResult> ResetPasswordAsync(string userId, string token, string newPassword);
}

public sealed record AccountSummary(
    string Id,
    string? Email,
    bool EmailConfirmed,
    string FullName,
    string FirstName,
    string LastName,
    bool IsActive,
    string? ImageUrl);

public sealed record NewUserDescriptor(
    string Email,
    string FirstName,
    string LastName,
    UserType UserType,
    string? PhoneNumber,
    bool IsActive);

public sealed record IdentityGatewayError(string Code, string Description);

public class IdentityGatewayResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<IdentityGatewayError> Errors { get; init; } = [];

    public static IdentityGatewayResult Success() => new() { Succeeded = true };

    public static IdentityGatewayResult Failed(params IdentityGatewayError[] errors) => new() { Errors = errors };
}

public sealed class IdentityGatewayResult<T> : IdentityGatewayResult
{
    public T? Value { get; init; }

    public static IdentityGatewayResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public new static IdentityGatewayResult<T> Failed(params IdentityGatewayError[] errors) => new() { Errors = errors };
}
