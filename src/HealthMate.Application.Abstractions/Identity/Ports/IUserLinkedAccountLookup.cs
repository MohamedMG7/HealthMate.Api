namespace HealthMate.Application.Abstractions.Identity.Ports;

public interface IUserLinkedAccountLookup
{
    Task<int?> GetPatientIdByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int?> GetHealthCareProviderIdByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int?> GetAdminIdByUserIdAsync(string userId, CancellationToken ct = default);
}
