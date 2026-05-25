using System.Globalization;
using System.Text;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Tools;

namespace HealthMate.Sina.Sessions;

public class ContextSummarizer : IContextSummarizer
{
    private readonly ISinaClinicalReader reader;
    private readonly ToolRegistry toolRegistry;

    public ContextSummarizer(ISinaClinicalReader reader, ToolRegistry toolRegistry)
    {
        this.reader = reader;
        this.toolRegistry = toolRegistry;
    }

    public async Task<string> BuildSystemMessageAsync(int patientId, CancellationToken ct)
    {
        var chart = await reader.GetPatientChartAsync(patientId, ct);
        var builder = new StringBuilder();
        builder.AppendLine(SystemPrompt.Text);
        builder.AppendLine();
        builder.AppendLine("Available tools: " + string.Join(", ", toolRegistry.GetSchemas().Select(s => s.Name)) + ".");

        if (chart is null)
        {
            builder.AppendLine();
            builder.AppendLine($"# Patient on record");
            builder.AppendLine($"- Patient #{patientId}: chart summary unavailable.");
            return builder.ToString();
        }

        builder.AppendLine();
        builder.AppendLine("# Patient on record");
        builder.Append("- Patient ").Append(chart.RecordId)
            .Append(", ").Append(chart.Gender.ToLowerInvariant())
            .Append(", age ").Append(chart.Age.ToString(CultureInfo.InvariantCulture))
            .Append(", governorate ").Append(chart.Governorate);
        if (chart.Bmi.HasValue)
        {
            builder.Append(", BMI ").Append(chart.Bmi.Value.ToString("0.0", CultureInfo.InvariantCulture));
        }
        builder.AppendLine();

        AppendList(builder, "Active conditions", chart.ActiveConditions.Select(c => $"[{c.RecordId}] {c.Name} ({c.Severity.ToLowerInvariant()})"));
        AppendList(builder, "Allergies", chart.Allergies.Select(a => $"[{a.RecordId}] {a.Substance} ({a.Severity.ToLowerInvariant()}{FormatOptional(a.Reaction)})"));
        AppendList(builder, "Current medications", chart.CurrentMedications.Select(m => $"{m.MedicineName} {m.Dosage} / {m.FrequencyInHours}h / {m.DurationInDays}d [{m.RecordId}]"));
        AppendList(builder, "Last 3 encounters", chart.RecentEncounters.Select(e => $"[{e.RecordId}] {e.Start:yyyy-MM-dd} {e.Reason}"));
        AppendList(builder, "Recent abnormal labs", chart.RecentAbnormalLabs.Select(l => $"{l.Name} {l.Value} {l.Unit} ({l.Abnormality}) [{l.RecordId}]"));

        return builder.ToString();
    }

    private static void AppendList(StringBuilder builder, string label, IEnumerable<string> values)
    {
        var materialized = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        builder.Append("- ").Append(label).Append(": ");
        builder.AppendLine(materialized.Length == 0 ? "none recorded" : string.Join("; ", materialized));
    }

    private static string FormatOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $" - {value}";
    }
}
