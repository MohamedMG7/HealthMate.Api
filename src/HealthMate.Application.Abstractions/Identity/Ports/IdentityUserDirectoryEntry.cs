namespace HealthMate.Application.Abstractions.Identity.Ports;

public sealed record IdentityUserDirectoryEntry(
    string Id,
    string? Email,
    bool EmailConfirmed,
    string FirstName,
    string LastName,
    string? ImageUrl,
    IdentityUserType UserType,
    bool IsActive);
