using HealthMate.Application.Common;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Encounters.Queries;
using HealthMate.Application.Encounters.Services;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Encounters.Handlers;

public sealed class ListPatientEncountersQueryHandler(
    IEncounterHistoryReader reader,
    ILogger<ListPatientEncountersQueryHandler> logger)
    : IHandler<ListPatientEncountersQuery, EncounterHistoryPage>
{
    public async Task<EncounterHistoryPage> HandleAsync(ListPatientEncountersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var result = await reader.ListForPatientAsync(request.PatientId, page, pageSize, ct);

        logger.LogInformation(
            "Listed encounters for patient {PatientId}: page={Page} pageSize={PageSize} itemCount={ItemCount}",
            request.PatientId, page, pageSize, result.Items.Count);

        return result;
    }
}
