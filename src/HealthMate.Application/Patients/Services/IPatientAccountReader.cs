namespace HealthMate.Application.Patients.Services;

public interface IPatientAccountReader
{
    Task<IReadOnlyDictionary<string, PatientAccountSummary>> GetByUserIdsAsync(IEnumerable<string?> userIds, CancellationToken ct);
}

public sealed record PatientAccountSummary(
    string UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? ImageUrl);
