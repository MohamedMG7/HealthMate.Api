namespace HealthMate.Fhir.Ports.Dtos;

public sealed record FhirPatientSnapshot(
    string FhirId,
    string NationalId,
    string? Name,
    DateOnly BirthDate,
    string Gender,
    string Governorate,
    string City,
    string? PhoneE164,
    string? Email,
    bool IsVerified,
    DateTimeOffset LastUpdated,
    uint VersionId,
    bool IsDeleted);
