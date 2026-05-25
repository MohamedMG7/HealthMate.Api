using HealthMate.Sina.Ports;
using HealthMate.Sina.Tools;
using HealthMate.Sina.Tools.DrugInteractions;

namespace HealthMate.Sina.Sessions;

public class ProactiveAlertEngine : IProactiveAlertEngine
{
    private readonly ISinaClinicalReader reader;
    private readonly IDrugInteractionLookup drugInteractionLookup;
    private readonly ISinaClock clock;

    public ProactiveAlertEngine(ISinaClinicalReader reader, IDrugInteractionLookup drugInteractionLookup, ISinaClock clock)
    {
        this.reader = reader;
        this.drugInteractionLookup = drugInteractionLookup;
        this.clock = clock;
    }

    public async Task<IReadOnlyList<SinaAlert>> ScanAsync(int patientId, CancellationToken ct)
    {
        var alerts = new List<SinaAlert>();
        var medications = await reader.GetActiveMedicationsAsync(patientId, ct);
        var allergies = await reader.GetActiveAllergiesAsync(patientId, ct);

        alerts.AddRange(drugInteractionLookup.FindInteractions(medications)
            .Where(i => IsHighRisk(i.Severity))
            .Select(i => new SinaAlert(
                "DrugInteraction",
                i.Severity,
                $"{i.DrugAName} [#PM-{i.DrugAId}] interacts with {i.DrugBName} [#PM-{i.DrugBId}]: {i.Description}",
                $"#PM-{i.DrugAId}")));

        foreach (var allergy in allergies)
        {
            foreach (var medication in medications)
            {
                if (Matches(allergy.Substance, medication))
                {
                    alerts.Add(new SinaAlert(
                        "AllergyConflict",
                        allergy.Severity,
                        $"{medication.MedicineName} [{medication.RecordId}] may conflict with allergy {allergy.Substance} [{allergy.RecordId}].",
                        medication.RecordId));
                }
            }
        }

        var chart = await reader.GetPatientChartAsync(patientId, ct);
        if (chart is not null)
        {
            var cutoff = clock.UtcNow().AddDays(-7);
            alerts.AddRange(chart.RecentAbnormalLabs
                .Where(l => l.RecordedAt >= cutoff && l.Abnormality is not "normal" and not "unknown")
                .Select(l => new SinaAlert(
                    "AbnormalLab",
                    LabRangeParser.IsSevere(l.Value, l.NormalRange) ? "severe" : "high",
                    $"{l.Name} {l.Value} {l.Unit} on {l.RecordedAt:yyyy-MM-dd} [{l.RecordId}] is {l.Abnormality} against normal range {l.NormalRange}.",
                    l.RecordId)));
        }

        return alerts
            .GroupBy(a => new { a.Type, a.RecordId, a.Message })
            .Select(g => g.First())
            .ToArray();
    }

    public string RenderAlerts(IReadOnlyList<SinaAlert> alerts)
    {
        if (alerts.Count == 0)
        {
            return "# Alerts on session open\n- No proactive alerts found in the available chart data.";
        }

        return "# Alerts on session open\n" + string.Join(Environment.NewLine, alerts.Select(a => $"- {a.Severity}: {a.Message}"));
    }

    private static bool IsHighRisk(string severity)
    {
        return severity.Equals("severe", StringComparison.OrdinalIgnoreCase)
            || severity.Equals("high", StringComparison.OrdinalIgnoreCase);
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
