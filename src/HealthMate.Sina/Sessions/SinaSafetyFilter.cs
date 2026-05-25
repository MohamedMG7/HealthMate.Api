using System.Text.RegularExpressions;

namespace HealthMate.Sina.Sessions;

public partial class SinaSafetyFilter : ISinaSafetyFilter
{
    private const string CitationWarning = "\n\nSina did not cite a source for at least one clinical claim; treat this as preliminary and verify against the chart.";
    private const string PhysicianJudgmentDisclaimer = "\n\nThe physician must make the final diagnosis and prescribing decision.";

    public bool TryBuildNonClinicalResponse(string userMessage, out string response)
    {
        var normalized = userMessage.Trim().ToLowerInvariant();
        if (NonClinicalRegex().IsMatch(normalized) && !ClinicalRegex().IsMatch(normalized))
        {
            response = "I can only help with clinical decision-support questions about the patient chart.";
            return true;
        }

        response = string.Empty;
        return false;
    }

    public string ApplyAssistantGuards(string userMessage, string assistantText)
    {
        var guarded = assistantText;
        if (LooksClinical(assistantText) && ExtractCitations(assistantText).Count == 0)
        {
            guarded += CitationWarning;
        }

        if (NeedsPhysicianJudgmentDisclaimer(userMessage, assistantText) && !guarded.Contains("physician", StringComparison.OrdinalIgnoreCase))
        {
            guarded += PhysicianJudgmentDisclaimer;
        }

        return guarded;
    }

    public IReadOnlyList<string> ExtractCitations(string text)
    {
        return CitationRegex().Matches(text)
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool LooksClinical(string text)
    {
        return ClinicalRegex().IsMatch(text);
    }

    private static bool NeedsPhysicianJudgmentDisclaimer(string userMessage, string assistantText)
    {
        return PrescribingOrDiagnosisRegex().IsMatch(userMessage) || DefinitiveClinicalActionRegex().IsMatch(assistantText);
    }

    [GeneratedRegex(@"\[#[-A-Za-z0-9]+\]")]
    private static partial Regex CitationRegex();

    [GeneratedRegex(@"\b(weather|joke|movie|football|sports|recipe|stock|music|travel)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NonClinicalRegex();

    [GeneratedRegex(@"\b(patient|lab|labs|diagnos|prescrib|medicine|medication|dose|allerg|symptom|treatment|condition|encounter|observation|HbA1c|hemoglobin|glucose|blood pressure)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ClinicalRegex();

    [GeneratedRegex(@"\b(diagnos|prescrib|start|add|stop|dose|medication|medicine)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PrescribingOrDiagnosisRegex();

    [GeneratedRegex(@"\b(definitely|must start|should start|prescribe|diagnosis is|has\s+[a-z]+\s+disease)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DefinitiveClinicalActionRegex();
}
