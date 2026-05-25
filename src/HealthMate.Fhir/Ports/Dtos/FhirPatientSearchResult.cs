namespace HealthMate.Fhir.Ports.Dtos;

public sealed record FhirPatientSearchResult(
    IReadOnlyList<FhirPatientSnapshot> Matches,
    int Total,
    int Offset,
    int Count);
