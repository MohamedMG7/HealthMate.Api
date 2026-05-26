using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResult>(
    ILogger<LoggingBehavior<TRequest, TResult>> logger,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var handler = typeof(TRequest).Name;
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value;
        var hcpId = httpContextAccessor.HttpContext?.User.FindFirst("HealthCareProviderId")?.Value;

        try
        {
            var result = await next();
            success = true;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Handled {Handler} for user {UserId} hcp {HcpId} in {LatencyMs} ms success {Success}",
                handler,
                userId,
                hcpId,
                stopwatch.ElapsedMilliseconds,
                success);
        }
    }
}
