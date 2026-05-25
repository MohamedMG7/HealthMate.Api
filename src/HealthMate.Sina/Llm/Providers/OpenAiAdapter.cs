using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace HealthMate.Sina.Llm.Providers;

public class OpenAiAdapter : IClinicalLlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient httpClient;
    private readonly SinaLlmConfig config;

    public OpenAiAdapter(HttpClient httpClient, IOptions<SinaLlmConfig> options)
    {
        this.httpClient = httpClient;
        config = options.Value;
    }

    public string ProviderName => "OpenAi";

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        var provider = config.OpenAi;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl(provider));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        httpRequest.Content = JsonContent.Create(BuildRequest(provider.Model, request), options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("OpenAI provider returned an unsuccessful status code.", null, response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return ParseResponse(document.RootElement);
    }

    private static string BuildUrl(ProviderEndpointConfig provider)
    {
        return provider.BaseUrl.TrimEnd('/') + "/v1/chat/completions";
    }

    private static OpenAiRequest BuildRequest(string model, LlmRequest request)
    {
        var messages = new List<OpenAiMessage>
        {
            new("system", request.SystemInstruction, null, null, null)
        };
        messages.AddRange(request.Messages.Select(ToOpenAiMessage));

        var tools = request.Tools.Count == 0
            ? null
            : request.Tools.Select(tool => new OpenAiTool("function", new OpenAiFunction(tool.Name, tool.Description, tool.ParametersSchema))).ToArray();

        return new OpenAiRequest(model, messages, tools, request.MaxOutputTokens, request.Temperature);
    }

    private static OpenAiMessage ToOpenAiMessage(LlmMessage message)
    {
        return message.Role switch
        {
            LlmRole.Assistant => new OpenAiMessage(
                "assistant",
                message.Text,
                null,
                null,
                message.ToolCalls?.Select(call => new OpenAiToolCall(
                    call.Id,
                    "function",
                    new OpenAiToolCallFunction(call.Name, call.Arguments.GetRawText()))).ToArray()),
            LlmRole.Tool => new OpenAiMessage("tool", message.Text ?? string.Empty, message.ToolCallId, message.ToolName, null),
            _ => new OpenAiMessage("user", message.Text ?? string.Empty, null, null, null)
        };
    }

    private static LlmResponse ParseResponse(JsonElement root)
    {
        var message = root.GetProperty("choices")[0].GetProperty("message");
        var text = message.TryGetProperty("content", out var content) && content.ValueKind != JsonValueKind.Null
            ? content.GetString()
            : null;

        var calls = new List<LlmToolCall>();
        if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (var toolCall in toolCalls.EnumerateArray())
            {
                var function = toolCall.GetProperty("function");
                var argsText = function.TryGetProperty("arguments", out var argsValue) ? argsValue.GetString() : "{}";
                calls.Add(new LlmToolCall(
                    toolCall.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
                    function.GetProperty("name").GetString() ?? "tool",
                    ParseArguments(argsText)));
            }
        }

        var finishReason = root.GetProperty("choices")[0].TryGetProperty("finish_reason", out var finish)
            ? MapFinishReason(finish.GetString())
            : LlmFinishReason.Other;

        return new LlmResponse(text, calls, ParseUsage(root), calls.Count > 0 ? LlmFinishReason.ToolCalls : finishReason);
    }

    private static JsonElement ParseArguments(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
            }
        }

        return JsonSerializer.SerializeToElement(new { }, JsonOptions);
    }

    private static LlmUsage ParseUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage))
        {
            return new LlmUsage(0, 0);
        }

        return new LlmUsage(
            usage.TryGetProperty("prompt_tokens", out var input) ? input.GetInt32() : 0,
            usage.TryGetProperty("completion_tokens", out var output) ? output.GetInt32() : 0);
    }

    private static LlmFinishReason MapFinishReason(string? reason) => reason?.ToLowerInvariant() switch
    {
        "stop" => LlmFinishReason.Stop,
        "tool_calls" => LlmFinishReason.ToolCalls,
        "length" => LlmFinishReason.MaxTokens,
        "content_filter" => LlmFinishReason.Safety,
        _ => LlmFinishReason.Other
    };

    private sealed record OpenAiRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiMessage> Messages,
        [property: JsonPropertyName("tools")] IReadOnlyList<OpenAiTool>? Tools,
        [property: JsonPropertyName("max_tokens")] int? MaxTokens,
        [property: JsonPropertyName("temperature")] double? Temperature);

    private sealed record OpenAiMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("tool_call_id")] string? ToolCallId,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("tool_calls")] IReadOnlyList<OpenAiToolCall>? ToolCalls);

    private sealed record OpenAiTool(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("function")] OpenAiFunction Function);

    private sealed record OpenAiFunction(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("parameters")] JsonElement Parameters);

    private sealed record OpenAiToolCall(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("function")] OpenAiToolCallFunction Function);

    private sealed record OpenAiToolCallFunction(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("arguments")] string Arguments);
}
