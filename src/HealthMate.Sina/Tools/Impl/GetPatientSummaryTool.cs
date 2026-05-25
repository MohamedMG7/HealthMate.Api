using System.Text.Json;
using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.Impl;

public class GetPatientSummaryTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;

    public GetPatientSummaryTool(ISinaClinicalReader reader)
    {
        this.reader = reader;
    }

    public string Name => "get_patient_summary";
    public string Description => "Fetch the patient's chart summary, active conditions, allergies, current medications, recent encounters, and recent abnormal labs.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema();

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var summary = await reader.GetPatientChartAsync(ctx.PatientId, ct);
        return summary is null
            ? ToolJson.ToJsonElement(new { error = "Patient chart was not found." })
            : ToolJson.ToJsonElement(summary);
    }
}
