namespace HealthMate.Application.Common;

public interface IHandler<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> HandleAsync(TRequest request, CancellationToken ct);
}
