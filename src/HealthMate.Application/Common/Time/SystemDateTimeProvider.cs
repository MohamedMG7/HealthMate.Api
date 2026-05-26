using HealthMate.Domain.Common;

namespace HealthMate.Application.Common.Time;

public sealed class SystemDateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
