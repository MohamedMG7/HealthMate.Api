using HealthMate.Fhir.Ports.Dtos;
using Hl7.Fhir.Model;

namespace HealthMate.Fhir.Mapping;

public sealed class OperationOutcomeFactory
{
    public OperationOutcome NotFound(string resourceType, string id) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.NotFound,
        $"{resourceType}/{id} was not found.");

    public OperationOutcome Gone(string resourceType, string id) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.Deleted,
        $"{resourceType}/{id} has been deleted.");

    public OperationOutcome Invalid(string detail, string? fhirPath = null) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.Invalid,
        detail,
        fhirPath);

    public OperationOutcome Invalid(IEnumerable<FhirValidationIssue> issues)
    {
        var outcome = new OperationOutcome();
        foreach (var issue in issues)
        {
            outcome.Issue.Add(BuildIssue(
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid,
                issue.Detail,
                issue.FhirPath));
        }

        if (outcome.Issue.Count == 0)
        {
            outcome.Issue.Add(BuildIssue(
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid,
                "FHIR resource is invalid.",
                null));
        }

        return outcome;
    }

    public OperationOutcome Conflict(string detail) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.Conflict,
        detail);

    public OperationOutcome PreconditionFailed(string detail) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.Conflict,
        detail);

    public OperationOutcome PreconditionRequired(string detail) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.Required,
        detail);

    public OperationOutcome Forbidden(string detail) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.Forbidden,
        detail);

    public OperationOutcome UnsupportedMediaType(string detail) => Single(
        OperationOutcome.IssueSeverity.Error,
        OperationOutcome.IssueType.NotSupported,
        detail);

    public OperationOutcome InternalError() => Single(
        OperationOutcome.IssueSeverity.Fatal,
        OperationOutcome.IssueType.Exception,
        "An internal FHIR server error occurred.");

    public OperationOutcome Valid() => Single(
        OperationOutcome.IssueSeverity.Information,
        OperationOutcome.IssueType.Informational,
        "Validation succeeded.");

    private static OperationOutcome Single(
        OperationOutcome.IssueSeverity severity,
        OperationOutcome.IssueType code,
        string detail,
        string? fhirPath = null)
    {
        return new OperationOutcome
        {
            Issue = [BuildIssue(severity, code, detail, fhirPath)]
        };
    }

    private static OperationOutcome.IssueComponent BuildIssue(
        OperationOutcome.IssueSeverity severity,
        OperationOutcome.IssueType code,
        string detail,
        string? fhirPath)
    {
        var issue = new OperationOutcome.IssueComponent
        {
            Severity = severity,
            Code = code,
            Diagnostics = detail
        };

        if (!string.IsNullOrWhiteSpace(fhirPath))
        {
            issue.Expression = [fhirPath];
        }

        return issue;
    }
}
