namespace HealthMate.Application.Common;

public delegate Task<TResult> RequestHandlerDelegate<TResult>();

public interface IPipelineBehavior<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct);
}
