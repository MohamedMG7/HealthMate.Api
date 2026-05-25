using System.Text.Json;
using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.Impl;

public class GetLabTestTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;

    public GetLabTestTool(ISinaClinicalReader reader)
    {
        this.reader = reader;
    }

    public string Name => "get_lab_test";
    public string Description => "Fetch one lab test by id, including each result value, unit, normal range, abnormality flag, and record id.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema(("lab_test_id", "integer", "The lab test id from the patient's chart.", true));

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var labTestId = ToolJson.GetInt(arguments, "lab_test_id");
        var lab = await reader.GetLabTestAsync(ctx.PatientId, labTestId, ct);
        return lab is null
            ? ToolJson.ToJsonElement(new { error = "Lab test was not found for this patient." })
            : ToolJson.ToJsonElement(lab);
    }
}
