namespace HealthMate.Fhir.Ports.Dtos;

public sealed record FhirPatientHistoryEntry(
    FhirPatientSnapshot Snapshot,
    FhirHistoryOperation Operation,
    DateTimeOffset RecordedAt);

public sealed record FhirPatientHistoryResult(IReadOnlyList<FhirPatientHistoryEntry> Entries, int Total, int Count);

public enum FhirHistoryOperation
{
    Create,
    Update,
    Delete
}
