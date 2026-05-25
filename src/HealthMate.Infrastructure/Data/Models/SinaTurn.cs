using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models;

public class SinaTurn
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public SinaSession Session { get; set; } = null!;
    public int OrdinalIndex { get; set; }
    public SinaTurnRole Role { get; set; }
    public string Content { get; set; } = null!;
    public string? ToolName { get; set; }
    public string? ToolCallId { get; set; }
    public DateTime CreatedAt { get; set; }
}
