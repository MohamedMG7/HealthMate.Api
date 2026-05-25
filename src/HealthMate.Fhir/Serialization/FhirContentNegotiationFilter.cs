using HealthMate.Fhir.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace HealthMate.Fhir.Serialization;

public sealed class FhirContentNegotiationFilter(
    OperationOutcomeFactory outcomes,
    FhirJsonService json) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.Path.StartsWithSegments("/fhir", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (!AcceptsFhirJson(request))
        {
            context.Result = Outcome(StatusCodes.Status415UnsupportedMediaType,
                outcomes.UnsupportedMediaType("FHIR endpoints serve application/fhir+json or application/json only."));
            return;
        }

        if (HasBody(request) && !HasSupportedContentType(request))
        {
            context.Result = Outcome(StatusCodes.Status415UnsupportedMediaType,
                outcomes.UnsupportedMediaType("FHIR request bodies must use application/fhir+json or application/json."));
            return;
        }

        await next();
    }

    private ContentResult Outcome(int statusCode, Hl7.Fhir.Model.OperationOutcome outcome)
    {
        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/fhir+json",
            Content = json.Serialize(outcome)
        };
    }

    private static bool AcceptsFhirJson(HttpRequest request)
    {
        if (request.Query.TryGetValue("_format", out var format)
            && format.Any(static v => v.Equals("json", StringComparison.OrdinalIgnoreCase)
                || v.Equals("application/fhir+json", StringComparison.OrdinalIgnoreCase)
                || v.Equals("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (request.Headers.Accept.Count == 0)
        {
            return true;
        }

        foreach (var value in request.Headers.Accept)
        {
            foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!MediaTypeHeaderValue.TryParse(part, out var mediaType))
                {
                    continue;
                }

                var media = mediaType.MediaType.Value;
                if (media is "*/*" or "application/*" or "application/json" or "application/fhir+json")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasSupportedContentType(HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            return false;
        }

        return MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            && mediaType.MediaType.Value is "application/json" or "application/fhir+json";
    }

    private static bool HasBody(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method);
    }
}
