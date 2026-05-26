namespace HealthMate.Domain.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
