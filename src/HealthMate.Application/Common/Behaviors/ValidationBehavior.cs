using FluentValidation;
using HealthMate.Application.Common.Exceptions;

namespace HealthMate.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResult>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        var validatorList = validators.ToArray();
        if (validatorList.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ApplicationValidationError>();

        foreach (var validator in validatorList)
        {
            var result = await validator.ValidateAsync(context, ct);
            failures.AddRange(result.Errors
                .Where(static failure => failure is not null)
                .Select(static failure => new ApplicationValidationError(failure.PropertyName, failure.ErrorMessage)));
        }

        if (failures.Count > 0)
        {
            throw new ApplicationValidationException(failures);
        }

        return await next();
    }
}
