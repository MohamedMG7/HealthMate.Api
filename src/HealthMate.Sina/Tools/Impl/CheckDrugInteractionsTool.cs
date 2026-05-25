using System.Text.Json;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Tools.DrugInteractions;

namespace HealthMate.Sina.Tools.Impl;

public class CheckDrugInteractionsTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;
    private readonly IDrugInteractionLookup lookup;

    public CheckDrugInteractionsTool(ISinaClinicalReader reader, IDrugInteractionLookup lookup)
    {
        this.reader = reader;
        this.lookup = lookup;
    }

    public string Name => "check_drug_interactions";
    public string Description => "Check current medications for known high-risk local drug interaction rules. Returns matching medication ids, severity, source, and description.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema();

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var medications = await reader.GetActiveMedicationsAsync(ctx.PatientId, ct);
        return ToolJson.ToJsonElement(lookup.FindInteractions(medications));
    }
}
