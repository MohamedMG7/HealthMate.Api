using HealthMate.Sina.Ports;

namespace HealthMate.Infrastructure.Sina;

public class SystemSinaClock : ISinaClock
{
    public DateTime UtcNow() => DateTime.UtcNow;
}
