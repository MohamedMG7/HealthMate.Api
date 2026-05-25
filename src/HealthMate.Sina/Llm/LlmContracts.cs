using System.Text.Json;

namespace HealthMate.Sina.Llm;

public record LlmRequest(
    string SystemInstruction,
    IReadOnlyList<LlmMessage> Messages,
    IReadOnlyList<LlmToolSchema> Tools,
    int? MaxOutputTokens = null,
    double? Temperature = null);

public record LlmMessage(
    LlmRole Role,
    string? Text,
    IReadOnlyList<LlmToolCall>? ToolCalls,
    string? ToolCallId,
    string? ToolName);

public record LlmToolCall(string Id, string Name, JsonElement Arguments);

public record LlmToolSchema(string Name, string Description, JsonElement ParametersSchema);

public record LlmResponse(
    string? Text,
    IReadOnlyList<LlmToolCall> ToolCalls,
    LlmUsage Usage,
    LlmFinishReason Finish);

public enum LlmRole
{
    User = 0,
    Assistant = 1,
    Tool = 2
}

public record LlmUsage(int InputTokens, int OutputTokens);

public enum LlmFinishReason
{
    Stop = 0,
    ToolCalls = 1,
    MaxTokens = 2,
    Safety = 3,
    Other = 4
}
