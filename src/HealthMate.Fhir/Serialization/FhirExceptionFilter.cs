using HealthMate.Fhir.Mapping;
using HealthMate.Fhir.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HealthMate.Fhir.Serialization;

public sealed class FhirExceptionFilter(
    OperationOutcomeFactory outcomes,
    FhirJsonService json) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/fhir", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var (statusCode, outcome) = context.Exception switch
        {
            FhirNotFoundException ex => (StatusCodes.Status404NotFound, outcomes.NotFound(ex.ResourceType, ex.Id)),
            FhirConcurrencyException => (StatusCodes.Status412PreconditionFailed, outcomes.PreconditionFailed("The supplied If-Match version is stale.")),
            FhirPreconditionRequiredException ex => (StatusCodes.Status428PreconditionRequired, outcomes.PreconditionRequired(ex.Message)),
            FhirValidationException ex => (StatusCodes.Status400BadRequest, outcomes.Invalid(ex.Issues)),
            _ => (StatusCodes.Status500InternalServerError, outcomes.InternalError())
        };

        context.Result = new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/fhir+json",
            Content = json.Serialize(outcome)
        };
        context.ExceptionHandled = true;
    }
}
