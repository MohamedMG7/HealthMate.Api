using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Sina.Llm;
using HealthMate.Sina.Llm.Providers;
using Microsoft.Extensions.Options;

namespace HealthMate.Tests.Sina.Llm;

public sealed class LlmAdapterTests
{
    [Fact]
    public async Task GeminiAdapter_parses_text_and_tool_calls()
    {
        var handler = new CapturingHandler("""
        {
          "candidates": [{
            "finishReason": "STOP",
            "content": { "parts": [
              { "text": "Need a lookup." },
              { "functionCall": { "name": "get_patient_summary", "args": {} } }
            ]}
          }],
          "usageMetadata": { "promptTokenCount": 4, "candidatesTokenCount": 3 }
        }
        """);
        var sut = new GeminiAdapter(new HttpClient(handler), Options.Create(Config()));

        var result = await sut.GenerateAsync(RequestWithToolHistory(), CancellationToken.None);

        result.Text.Should().Contain("Need a lookup");
        result.ToolCalls.Should().ContainSingle(c => c.Name == "get_patient_summary");
        result.ToolCalls[0].Id.Should().StartWith("g:");
        handler.RequestBody.Should().Contain("functionResponse");
    }

    [Fact]
    public async Task OpenAiAdapter_parses_text_and_tool_calls()
    {
        var handler = new CapturingHandler("""
        {
          "choices": [{
            "finish_reason": "tool_calls",
            "message": {
              "role": "assistant",
              "content": "Need a lookup.",
              "tool_calls": [{
                "id": "call_1",
                "type": "function",
                "function": { "name": "get_patient_summary", "arguments": "{}" }
              }]
            }
          }],
          "usage": { "prompt_tokens": 5, "completion_tokens": 4 }
        }
        """);
        var sut = new OpenAiAdapter(new HttpClient(handler), Options.Create(Config()));

        var result = await sut.GenerateAsync(RequestWithToolHistory(), CancellationToken.None);

        result.Text.Should().Contain("Need a lookup");
        result.ToolCalls.Should().ContainSingle(c => c.Id == "call_1");
        handler.RequestBody.Should().Contain("tool_call_id");
    }

    [Theory]
    [InlineData("Gemini")]
    [InlineData("OpenAi")]
    public async Task Adapters_throw_on_provider_errors(string provider)
    {
        var handler = new CapturingHandler("{}", HttpStatusCode.BadGateway);
        IClinicalLlmClient sut = provider == "Gemini"
            ? new GeminiAdapter(new HttpClient(handler), Options.Create(Config()))
            : new OpenAiAdapter(new HttpClient(handler), Options.Create(Config()));

        await sut.Invoking(s => s.GenerateAsync(RequestWithToolHistory(), CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    private static LlmRequest RequestWithToolHistory()
    {
        var toolSchema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new { }
        });
        var toolArgs = JsonSerializer.SerializeToElement(new { });
        return new LlmRequest(
            "system",
            [
                new LlmMessage(LlmRole.User, "summarize", null, null, null),
                new LlmMessage(LlmRole.Assistant, null, [new LlmToolCall("call_existing", "get_patient_summary", toolArgs)], null, null),
                new LlmMessage(LlmRole.Tool, "{\"record_id\":\"#P-1\"}", null, "call_existing", "get_patient_summary")
            ],
            [new LlmToolSchema("get_patient_summary", "summary", toolSchema)]);
    }

    private static SinaLlmConfig Config()
    {
        return new SinaLlmConfig
        {
            Gemini = new ProviderEndpointConfig { ApiKey = "test-key", BaseUrl = "https://gemini.invalid/", Model = "gemini-test" },
            OpenAi = new ProviderEndpointConfig { ApiKey = "test-key", BaseUrl = "https://openai.invalid", Model = "openai-test" }
        };
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string responseJson;
        private readonly HttpStatusCode statusCode;

        public CapturingHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            this.responseJson = responseJson;
            this.statusCode = statusCode;
        }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
