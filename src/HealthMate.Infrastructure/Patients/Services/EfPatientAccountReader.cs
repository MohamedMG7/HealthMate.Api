using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Application.Patients.Services;

public sealed class EfPatientAccountReader(HealthMateContext context) : IPatientAccountReader
{
    public async Task<IReadOnlyDictionary<string, PatientAccountSummary>> GetByUserIdsAsync(IEnumerable<string?> userIds, CancellationToken ct)
    {
        var ids = userIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<string, PatientAccountSummary>(StringComparer.Ordinal);
        }

        var users = await context.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new PatientAccountSummary(
                user.Id,
                user.First_Name,
                user.Last_Name,
                user.Email,
                user.ImageUrl))
            .ToListAsync(ct);

        return users.ToDictionary(user => user.UserId, StringComparer.Ordinal);
    }
}
