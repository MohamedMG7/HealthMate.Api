namespace HealthMate.Fhir.Ports.Dtos;

public sealed record FhirValidationIssue(string Detail, string? FhirPath = null);
