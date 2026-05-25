using System.Text.Json;
using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.Impl;

public class SearchObservationsTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;

    public SearchObservationsTool(ISinaClinicalReader reader)
    {
        this.reader = reader;
    }

    public string Name => "search_observations";
    public string Description => "Search patient observations by code or display text, with optional date bounds and a result limit.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema(
        ("code_or_display", "string", "Observation code or display text to search for.", true),
        ("from", "string", "Optional ISO date lower bound.", false),
        ("to", "string", "Optional ISO date upper bound.", false),
        ("limit", "integer", "Maximum number of observations to return. Default 10.", false));

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var term = ToolJson.GetString(arguments, "code_or_display") ?? string.Empty;
        var from = ToolJson.GetDate(arguments, "from");
        var to = ToolJson.GetDate(arguments, "to");
        var limit = ToolJson.GetInt(arguments, "limit", 10);
        var observations = await reader.SearchObservationsAsync(ctx.PatientId, term, from, to, Math.Clamp(limit, 1, 50), ct);
        return ToolJson.ToJsonElement(observations);
    }
}
