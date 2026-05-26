using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Identity.Repositories;

public sealed class IdentityUserDirectory : IIdentityUserDirectory
{
    private readonly IApplicationUserRepo _users;

    public IdentityUserDirectory(IApplicationUserRepo users)
    {
        _users = users;
    }

    public IReadOnlyList<IdentityUserDirectoryEntry> GetAll()
    {
        return _users.GetAll().Select(Map).ToList();
    }

    public IReadOnlyList<IdentityUserDirectoryEntry> GetAllActive()
    {
        return _users.GetAll()
            .Where(user => user.IsActive == true)
            .Select(Map)
            .ToList();
    }

    public string? GetUserNameById(string id)
    {
        return _users.GetUsernameById(id);
    }

    private static IdentityUserDirectoryEntry Map(ApplicationUser user)
    {
        return new IdentityUserDirectoryEntry(
            user.Id,
            user.Email,
            user.EmailConfirmed,
            user.First_Name,
            user.Last_Name,
            user.ImageUrl,
            (IdentityUserType)user.UserType,
            user.IsActive);
    }
}
