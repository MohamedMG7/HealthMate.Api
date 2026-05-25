using System.Text.Json;
using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.Impl;

public class CheckAllergyConflictTool : ISinaTool
{
    private readonly ISinaClinicalReader reader;

    public CheckAllergyConflictTool(ISinaClinicalReader reader)
    {
        this.reader = reader;
    }

    public string Name => "check_allergy_conflict";
    public string Description => "Check active allergies against one current medicine or all current medicines using substance substring matching.";
    public JsonElement ParametersSchema => ToolJson.ObjectSchema(("medicine_id", "integer", "Optional medicine id. If omitted, checks all current medications.", false));

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var requestedMedicineId = ToolJson.GetInt(arguments, "medicine_id", 0);
        var allergies = await reader.GetActiveAllergiesAsync(ctx.PatientId, ct);
        var medications = await reader.GetActiveMedicationsAsync(ctx.PatientId, ct);

        if (requestedMedicineId > 0)
        {
            medications = medications.Where(m => m.MedicineId == requestedMedicineId).ToArray();
        }

        var conflicts = new List<object>();
        foreach (var allergy in allergies)
        {
            foreach (var medication in medications)
            {
                if (Matches(allergy.Substance, medication))
                {
                    conflicts.Add(new
                    {
                        allergy_id = allergy.Id,
                        allergy_record_id = allergy.RecordId,
                        medicine_id = medication.MedicineId,
                        patient_medicine_id = medication.PatientMedicineId,
                        medicine_record_id = medication.RecordId,
                        substance = allergy.Substance,
                        medicine_name = medication.MedicineName,
                        severity = allergy.Severity
                    });
                }
            }
        }

        return ToolJson.ToJsonElement(conflicts);
    }

    private static bool Matches(string substance, ActiveMedicationSummary medication)
    {
        return Contains(medication.MedicineName, substance) || Contains(medication.ActiveIngredients, substance);
    }

    private static bool Contains(string? haystack, string needle)
    {
        return !string.IsNullOrWhiteSpace(needle)
            && haystack?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;
    }
}
