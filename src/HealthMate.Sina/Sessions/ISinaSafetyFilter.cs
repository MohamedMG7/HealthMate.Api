namespace HealthMate.Sina.Sessions;

public interface ISinaSafetyFilter
{
    bool TryBuildNonClinicalResponse(string userMessage, out string response);
    string ApplyAssistantGuards(string userMessage, string assistantText);
    IReadOnlyList<string> ExtractCitations(string text);
}
