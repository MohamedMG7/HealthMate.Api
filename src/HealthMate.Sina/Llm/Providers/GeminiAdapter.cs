using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace HealthMate.Sina.Llm.Providers;

public class GeminiAdapter : IClinicalLlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient httpClient;
    private readonly SinaLlmConfig config;

    public GeminiAdapter(HttpClient httpClient, IOptions<SinaLlmConfig> options)
    {
        this.httpClient = httpClient;
        config = options.Value;
    }

    public string ProviderName => "Gemini";

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        var provider = config.Gemini;
        var url = BuildUrl(provider);
        var body = BuildRequest(request);

        using var response = await httpClient.PostAsJsonAsync(url, body, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("Gemini provider returned an unsuccessful status code.", null, response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return ParseResponse(document.RootElement, request.Messages.Count);
    }

    private static string BuildUrl(ProviderEndpointConfig provider)
    {
        var baseUrl = provider.BaseUrl.TrimEnd('/') + "/";
        return $"{baseUrl}models/{Uri.EscapeDataString(provider.Model)}:generateContent?key={Uri.EscapeDataString(provider.ApiKey)}";
    }

    private static GeminiRequest BuildRequest(LlmRequest request)
    {
        var contents = request.Messages.Select(ToGeminiContent).ToList();
        var tools = request.Tools.Count == 0
            ? null
            : new[]
            {
                new GeminiTool(request.Tools.Select(tool => new GeminiFunctionDeclaration(
                    tool.Name,
                    tool.Description,
                    tool.ParametersSchema)).ToArray())
            };

        return new GeminiRequest(
            new GeminiSystemInstruction([new GeminiPart(Text: request.SystemInstruction)]),
            contents,
            tools,
            new GeminiGenerationConfig(request.MaxOutputTokens, request.Temperature));
    }

    private static GeminiContent ToGeminiContent(LlmMessage message)
    {
        if (message.Role == LlmRole.Tool)
        {
            var response = ParseJsonOrText(message.Text);
            return new GeminiContent("user", [new GeminiPart(FunctionResponse: new GeminiFunctionResponse(message.ToolName ?? "tool", response))]);
        }

        var parts = new List<GeminiPart>();
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            parts.Add(new GeminiPart(Text: message.Text));
        }

        if (message.ToolCalls is { Count: > 0 })
        {
            parts.AddRange(message.ToolCalls.Select(call =>
                new GeminiPart(FunctionCall: new GeminiFunctionCall(call.Name, call.Arguments))));
        }

        var role = message.Role == LlmRole.Assistant ? "model" : "user";
        return new GeminiContent(role, parts);
    }

    private static JsonElement ParseJsonOrText(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
            }
        }

        return JsonSerializer.SerializeToElement(new { text = text ?? string.Empty }, JsonOptions);
    }

    private static LlmResponse ParseResponse(JsonElement root, int turnOrdinal)
    {
        var textParts = new List<string>();
        var toolCalls = new List<LlmToolCall>();
        var finishReason = LlmFinishReason.Other;

        if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
        {
            var candidate = candidates[0];
            finishReason = MapFinishReason(candidate.TryGetProperty("finishReason", out var finish) ? finish.GetString() : null);

            if (candidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                    {
                        var value = text.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            textParts.Add(value);
                        }
                    }

                    if (part.TryGetProperty("functionCall", out var functionCall))
                    {
                        var name = functionCall.GetProperty("name").GetString() ?? "tool";
                        var args = functionCall.TryGetProperty("args", out var argsElement)
                            ? argsElement.Clone()
                            : JsonSerializer.SerializeToElement(new { }, JsonOptions);
                        toolCalls.Add(new LlmToolCall($"g:{turnOrdinal}:{name}:{index}", name, args));
                        index++;
                    }
                }
            }
        }

        var usage = ParseUsage(root);
        return new LlmResponse(
            textParts.Count == 0 ? null : string.Join(Environment.NewLine, textParts),
            toolCalls,
            usage,
            toolCalls.Count > 0 ? LlmFinishReason.ToolCalls : finishReason);
    }

    private static LlmUsage ParseUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usageMetadata", out var usage))
        {
            return new LlmUsage(0, 0);
        }

        return new LlmUsage(
            usage.TryGetProperty("promptTokenCount", out var input) ? input.GetInt32() : 0,
            usage.TryGetProperty("candidatesTokenCount", out var output) ? output.GetInt32() : 0);
    }

    private static LlmFinishReason MapFinishReason(string? reason) => reason?.ToUpperInvariant() switch
    {
        "STOP" => LlmFinishReason.Stop,
        "MAX_TOKENS" => LlmFinishReason.MaxTokens,
        "SAFETY" or "RECITATION" => LlmFinishReason.Safety,
        _ => LlmFinishReason.Other
    };

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiSystemInstruction SystemInstruction,
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("tools")] IReadOnlyList<GeminiTool>? Tools,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiSystemInstruction([property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string? Text = null,
        [property: JsonPropertyName("functionCall")] GeminiFunctionCall? FunctionCall = null,
        [property: JsonPropertyName("functionResponse")] GeminiFunctionResponse? FunctionResponse = null);

    private sealed record GeminiTool([property: JsonPropertyName("functionDeclarations")] IReadOnlyList<GeminiFunctionDeclaration> FunctionDeclarations);

    private sealed record GeminiFunctionDeclaration(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("parameters")] JsonElement Parameters);

    private sealed record GeminiFunctionCall(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("args")] JsonElement Args);

    private sealed record GeminiFunctionResponse(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("response")] JsonElement Response);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("maxOutputTokens")] int? MaxOutputTokens,
        [property: JsonPropertyName("temperature")] double? Temperature);
}
