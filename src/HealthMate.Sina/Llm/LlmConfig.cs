namespace HealthMate.Sina.Llm;

public class SinaLlmConfig
{
    public const string SectionName = "Sina";

    public string Provider { get; set; } = "Gemini";
    public int MaxToolCallsPerTurn { get; set; } = 8;
    public ProviderEndpointConfig Gemini { get; set; } = new()
    {
        BaseUrl = "https://generativelanguage.googleapis.com/v1beta/",
        Model = "gemini-1.5-pro"
    };
    public ProviderEndpointConfig OpenAi { get; set; } = new()
    {
        BaseUrl = "https://api.openai.com",
        Model = "gpt-4o-mini"
    };
}

public class ProviderEndpointConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
