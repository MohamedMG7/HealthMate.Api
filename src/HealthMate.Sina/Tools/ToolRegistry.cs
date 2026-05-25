using System.Text.Json;
using HealthMate.Sina.Llm;

namespace HealthMate.Sina.Tools;

public class ToolRegistry
{
    private readonly IReadOnlyDictionary<string, ISinaTool> tools;

    public ToolRegistry(IEnumerable<ISinaTool> tools)
    {
        this.tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<LlmToolSchema> GetSchemas()
    {
        return tools.Values
            .Select(tool => new LlmToolSchema(tool.Name, tool.Description, tool.ParametersSchema))
            .ToArray();
    }

    public Task<JsonElement> ExecuteAsync(string name, JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        if (!tools.TryGetValue(name, out var tool))
        {
            return Task.FromResult(ToolJson.ToJsonElement(new { error = $"Unknown Sina tool '{name}'." }));
        }

        return tool.ExecuteAsync(arguments, ctx, ct);
    }
}
