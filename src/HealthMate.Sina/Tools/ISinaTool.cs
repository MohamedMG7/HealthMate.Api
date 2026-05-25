using System.Text.Json;

namespace HealthMate.Sina.Tools;

public interface ISinaTool
{
    string Name { get; }
    string Description { get; }
    JsonElement ParametersSchema { get; }
    Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct);
}

public record ToolExecutionContext(int PatientId, int HealthCareProviderId);
