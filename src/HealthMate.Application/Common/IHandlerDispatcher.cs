namespace HealthMate.Application.Common;

public interface IHandlerDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IRequest<TResult> request, CancellationToken ct = default);
}
