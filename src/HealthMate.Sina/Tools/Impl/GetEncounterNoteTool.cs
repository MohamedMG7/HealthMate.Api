using System.Text.Json;
using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.Impl;

public class GetEncounterNoteTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;

    public GetEncounterNoteTool(ISinaClinicalReader reader)
    {
        this.reader = reader;
    }

    public string Name => "get_encounter_note";
    public string Description => "Fetch a specific encounter note by id for this patient.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema(("encounter_id", "integer", "Encounter id from the patient's chart.", true));

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var encounterId = ToolJson.GetInt(arguments, "encounter_id");
        var encounter = await reader.GetEncounterNoteAsync(ctx.PatientId, encounterId, ct);
        return encounter is null
            ? ToolJson.ToJsonElement(new { error = "Encounter was not found for this patient." })
            : ToolJson.ToJsonElement(encounter);
    }
}
