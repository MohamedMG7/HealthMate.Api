using Microsoft.Extensions.Options;
using HealthMate.Sina.Llm.Providers;

namespace HealthMate.Sina.Llm;

public interface ILlmProviderSelector
{
    IClinicalLlmClient GetClient();
}

public class LlmProviderSelector : ILlmProviderSelector
{
    private readonly SinaLlmConfig config;
    private readonly GeminiAdapter gemini;
    private readonly OpenAiAdapter openAi;

    public LlmProviderSelector(IOptions<SinaLlmConfig> options, GeminiAdapter gemini, OpenAiAdapter openAi)
    {
        config = options.Value;
        this.gemini = gemini;
        this.openAi = openAi;
    }

    public IClinicalLlmClient GetClient()
    {
        if (string.Equals(config.Provider, gemini.ProviderName, StringComparison.OrdinalIgnoreCase))
        {
            EnsureConfigured(config.Gemini.ApiKey, gemini.ProviderName);
            return gemini;
        }

        if (string.Equals(config.Provider, openAi.ProviderName, StringComparison.OrdinalIgnoreCase))
        {
            EnsureConfigured(config.OpenAi.ApiKey, openAi.ProviderName);
            return openAi;
        }

        throw new InvalidOperationException($"Sina provider '{config.Provider}' is not supported.");
    }

    private static void EnsureConfigured(string apiKey, string providerName)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"Sina provider '{providerName}' is selected but its API key is not configured.");
        }
    }
}
