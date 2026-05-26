namespace HealthMate.Application.Common.Exceptions;

public sealed class ApplicationValidationException : ApplicationException
{
    public ApplicationValidationException(IEnumerable<ApplicationValidationError> errors)
        : base("The request could not be processed.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<ApplicationValidationError> Errors { get; }
}

public sealed record ApplicationValidationError(string Field, string Message);
