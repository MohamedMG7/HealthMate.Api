using HealthMate.Domain.Common;

namespace HealthMate.Tests.Infrastructure;

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public static readonly FixedDateTimeProvider Instance = new();

    private FixedDateTimeProvider()
    {
    }

    public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
