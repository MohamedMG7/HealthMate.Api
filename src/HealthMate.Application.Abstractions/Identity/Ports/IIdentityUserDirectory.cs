namespace HealthMate.Application.Abstractions.Identity.Ports;

public interface IIdentityUserDirectory
{
    IReadOnlyList<IdentityUserDirectoryEntry> GetAll();
    IReadOnlyList<IdentityUserDirectoryEntry> GetAllActive();
    string? GetUserNameById(string id);
}
