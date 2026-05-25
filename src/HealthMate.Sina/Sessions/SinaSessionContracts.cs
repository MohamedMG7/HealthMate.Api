using System.Text.Json;

namespace HealthMate.Sina.Sessions;

public record SinaSessionDto(
    Guid Id,
    int PatientId,
    int HealthCareProviderId,
    DateTime StartedAt,
    DateTime LastInteractionAt,
    SinaSessionStatus Status,
    IReadOnlyList<SinaTurnDto> Turns);

public record SinaTurnDto(
    Guid Id,
    Guid SessionId,
    int OrdinalIndex,
    SinaTurnRole Role,
    string Content,
    string? ToolName,
    string? ToolCallId,
    DateTime CreatedAt);

public record SinaTurnCreate(
    SinaTurnRole Role,
    string Content,
    string? ToolName = null,
    string? ToolCallId = null);

public enum SinaSessionStatus
{
    Active = 0,
    Closed = 1
}

public enum SinaTurnRole
{
    System = 0,
    User = 1,
    Assistant = 2,
    Tool = 3
}

public record SinaAlert(
    string Type,
    string Severity,
    string Message,
    string? RecordId);

public record OpenSessionResponse(
    Guid SessionId,
    IReadOnlyList<SinaTurnView> Turns,
    IReadOnlyList<SinaAlert> Alerts);

public record SinaTurnResponse(
    string Reply,
    IReadOnlyList<SinaTurnView> Turns,
    IReadOnlyList<string> Citations);

public record SinaTurnView(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    string? ToolName = null,
    string? ToolCallId = null);

public sealed class SinaUnavailableException : Exception
{
    public SinaUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
