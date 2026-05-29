using HealthMate.Application.Encounters.Contracts;

namespace HealthMate.Application.Encounters.Services;

public interface IEncounterHistoryReader
{
    Task<EncounterHistoryPage> ListForPatientAsync(
        int patientId, int page, int pageSize, CancellationToken ct);
}
