using System.Net.Mime;
using HealthMate.Application.Common.Exceptions;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Common;
using HealthMate.Fhir.Ports;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly Dictionary<Type, (int StatusCode, string Code, string Title)> DomainMappings = new()
    {
        [typeof(EncounterNotFoundException)] = (StatusCodes.Status404NotFound, "encounter_not_found", "The encounter could not be found."),
        [typeof(ConditionNotFoundException)] = (StatusCodes.Status404NotFound, "condition_not_found", "The condition could not be found."),
        [typeof(DiseaseNotFoundForConditionException)] = (StatusCodes.Status404NotFound, "disease_not_found_for_condition", "The disease could not be found."),
        [typeof(PatientNotFoundForEncounterException)] = (StatusCodes.Status404NotFound, "patient_not_found_for_encounter", "The patient could not be found."),
        [typeof(HealthCareProviderNotFoundForEncounterException)] = (StatusCodes.Status404NotFound, "health_care_provider_not_found_for_encounter", "The health care provider could not be found."),
        [typeof(PatientNotFoundException)] = (StatusCodes.Status404NotFound, "patient_not_found", "The patient could not be found."),
        [typeof(PatientAlreadyExistsException)] = (StatusCodes.Status409Conflict, "patient_conflict", "The patient conflicts with existing data.")
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var (statusCode, code, title, detail, errors) = MapException(exception, traceId);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled API exception {TraceId}", traceId);
        }
        else
        {
            logger.LogWarning(exception, "Handled API exception {TraceId} {Code}", traceId, code);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = traceId;
        if (errors.Count > 0)
        {
            problem.Extensions["errors"] = errors;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }

    private (int StatusCode, string Code, string Title, string? Detail, IReadOnlyList<ApiErrorItem> Errors) MapException(Exception exception, string traceId)
    {
        return exception switch
        {
            ApplicationValidationException validation => (
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "The request could not be processed.",
                validation.Message,
                validation.Errors.Select(static error => new ApiErrorItem(error.Field, error.Message)).ToArray()),

            FhirNotFoundException => (
                StatusCodes.Status404NotFound,
                "fhir_not_found",
                "The FHIR resource could not be found.",
                exception.Message,
                []),

            FhirConcurrencyException => (
                StatusCodes.Status409Conflict,
                "fhir_concurrency_conflict",
                "The FHIR resource version did not match.",
                exception.Message,
                []),

            DomainException domain => MapDomainException(domain),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "forbidden",
                "The request is not allowed.",
                "The authenticated user is not allowed to perform this action.",
                []),

            OperationCanceledException => (
                499,
                "request_cancelled",
                "The request was cancelled.",
                null,
                []),

            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "An unexpected error occurred.",
                environment.IsDevelopment() ? exception.Message : $"Reference trace id: {traceId}.",
                [])
        };
    }

    private static (int StatusCode, string Code, string Title, string? Detail, IReadOnlyList<ApiErrorItem> Errors) MapDomainException(DomainException exception)
    {
        if (DomainMappings.TryGetValue(exception.GetType(), out var mapping))
        {
            return (mapping.StatusCode, mapping.Code, mapping.Title, exception.Message, []);
        }

        var typeName = exception.GetType().Name;
        if (typeName.EndsWith("NotFoundException", StringComparison.Ordinal))
        {
            return (StatusCodes.Status404NotFound, "resource_not_found", "The resource could not be found.", exception.Message, []);
        }

        if (typeName.EndsWith("AlreadyExistsException", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "resource_conflict", "The resource conflicts with existing data.", exception.Message, []);
        }

        return (StatusCodes.Status422UnprocessableEntity, "domain_rule_failed", "A domain rule was violated.", exception.Message, []);
    }
}
