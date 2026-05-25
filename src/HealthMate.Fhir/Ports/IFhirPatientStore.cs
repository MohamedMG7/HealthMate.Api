using HealthMate.Fhir.Ports.Dtos;

namespace HealthMate.Fhir.Ports;

public interface IFhirPatientStore
{
    Task<FhirPatientSnapshot?> ReadAsync(string fhirId, CancellationToken ct);

    Task<FhirPatientSearchResult> SearchAsync(FhirPatientSearchQuery query, CancellationToken ct);

    Task<FhirPatientSnapshot> CreateAsync(FhirPatientSnapshot snapshot, CancellationToken ct);

    Task<FhirPatientSnapshot> UpdateAsync(FhirPatientSnapshot snapshot, uint expectedVersion, CancellationToken ct);

    Task DeleteAsync(string fhirId, uint? expectedVersion, CancellationToken ct);

    Task<FhirPatientHistoryResult> ReadHistoryAsync(string fhirId, int count, DateTimeOffset? since, CancellationToken ct);

    Task<FhirPatientHistoryEntry?> ReadVersionAsync(string fhirId, uint versionId, CancellationToken ct);
}
