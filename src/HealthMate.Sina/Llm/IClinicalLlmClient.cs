namespace HealthMate.Sina.Llm;

public interface IClinicalLlmClient
{
    string ProviderName { get; }
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct);
}
