using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HealthMate.Application.Common;

public sealed class HandlerDispatcher(IServiceProvider serviceProvider) : IHandlerDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(IRequest<TResult> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = typeof(HandlerDispatcher)
            .GetMethod(nameof(DispatchCoreAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(request.GetType(), typeof(TResult));

        return (Task<TResult>)method.Invoke(this, [request, ct])!;
    }

    private Task<TResult> DispatchCoreAsync<TRequest, TResult>(TRequest request, CancellationToken ct)
        where TRequest : IRequest<TResult>
    {
        var handler = serviceProvider.GetRequiredService<IHandler<TRequest, TResult>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResult>>().Reverse().ToArray();

        RequestHandlerDelegate<TResult> next = () => handler.HandleAsync(request, ct);
        foreach (var behavior in behaviors)
        {
            var current = next;
            next = () => behavior.HandleAsync(request, current, ct);
        }

        return next();
    }
}
