using HealthMate.Fhir.Ports.Dtos;

namespace HealthMate.Fhir.Ports;

public abstract class FhirException(string message) : Exception(message);

public sealed class FhirNotFoundException(string resourceType, string id)
    : FhirException($"{resourceType}/{id} was not found.")
{
    public string ResourceType { get; } = resourceType;
    public string Id { get; } = id;
}

public sealed class FhirConcurrencyException(string resourceType, string id)
    : FhirException($"{resourceType}/{id} version did not match the supplied If-Match value.")
{
    public string ResourceType { get; } = resourceType;
    public string Id { get; } = id;
}

public sealed class FhirPreconditionRequiredException(string detail) : FhirException(detail);

public class FhirValidationException(IReadOnlyList<FhirValidationIssue> issues)
    : FhirException(issues.Count == 0 ? "FHIR resource is invalid." : issues[0].Detail)
{
    public IReadOnlyList<FhirValidationIssue> Issues { get; } = issues;

    public FhirValidationException(string detail, string? fhirPath = null)
        : this([new FhirValidationIssue(detail, fhirPath)])
    {
    }
}

public sealed class FhirSearchParseException(string detail, string? fhirPath = null)
    : FhirValidationException(detail, fhirPath);
