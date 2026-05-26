namespace HealthMate.Api.Middleware;

public sealed record ApiError(
    string Code,
    string Title,
    string? Detail,
    string TraceId,
    IReadOnlyList<ApiErrorItem> Errors);

public sealed record ApiErrorItem(string Field, string Message);
