using System.Text.Json;
using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.Impl;

public class GetPrescriptionHistoryTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;

    public GetPrescriptionHistoryTool(ISinaClinicalReader reader)
    {
        this.reader = reader;
    }

    public string Name => "get_prescription_history";
    public string Description => "Fetch the patient's prescription history, optionally filtered by medicine name.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema(("medicine_name", "string", "Optional medicine name to filter prescriptions.", false));

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var medicineName = ToolJson.GetString(arguments, "medicine_name");
        var prescriptions = await reader.GetPrescriptionHistoryAsync(ctx.PatientId, medicineName, ct);
        return ToolJson.ToJsonElement(prescriptions);
    }
}
