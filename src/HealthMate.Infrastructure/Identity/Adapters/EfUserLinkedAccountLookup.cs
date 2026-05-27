using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Identity.Adapters;

public sealed class EfUserLinkedAccountLookup(HealthMateContext context) : IUserLinkedAccountLookup
{
    public Task<int?> GetPatientIdByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return context.Patients
            .AsNoTracking()
            .Where(patient => patient.ApplicationUserId == userId)
            .Select(patient => (int?)patient.Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task<int?> GetHealthCareProviderIdByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return context.HealthCareProviders
            .AsNoTracking()
            .Where(provider => provider.ApplicationUserId == userId)
            .Select(provider => (int?)provider.HealthCareProvider_Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task<int?> GetAdminIdByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return context.Admins
            .AsNoTracking()
            .Where(admin => admin.ApplicationUserId == userId)
            .Select(admin => (int?)admin.Admin_Id)
            .FirstOrDefaultAsync(ct);
    }
}
