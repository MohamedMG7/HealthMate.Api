using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Encounters.Services;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Persistence.Readers;

public sealed class EfEncounterHistoryReader(HealthMateContext context) : IEncounterHistoryReader
{
    public async Task<EncounterHistoryPage> ListForPatientAsync(
        int patientId, int page, int pageSize, CancellationToken ct)
    {
        var skip = (page - 1) * pageSize;
        var rows = await context.Encounters
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && !e.IsDeleted)
            .OrderByDescending(e => e.StartDate)
            .Skip(skip)
            .Take(pageSize + 1)
            .Select(e => new EncounterHistoryItem(
                e.Id,
                e.StartDate,
                e.EndDate,
                e.Status,
                e.ReasonToVisit.Value,
                e.HealthCareProviderId))
            .ToArrayAsync(ct);

        var hasMore = rows.Length > pageSize;
        var items = hasMore ? rows.Take(pageSize).ToArray() : rows;
        return new EncounterHistoryPage(items, page, pageSize, hasMore);
    }
}
