using System.Text.Json;
using FluentAssertions;
using HealthMate.Sina.Llm;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Sessions;
using HealthMate.Sina.Tools;
using Microsoft.Extensions.Options;

namespace HealthMate.Tests.Sina;

public sealed class SinaManagerTests
{
    [Fact]
    public async Task SendUserMessage_persists_assistant_reply_without_tool_call()
    {
        var harness = new Harness([new LlmResponse("The result is high [#LR-1].", [], new LlmUsage(0, 0), LlmFinishReason.Stop)]);
        var session = await harness.Sut.OpenOrResumeSessionAsync(1, 2, CancellationToken.None);

        var response = await harness.Sut.SendUserMessageAsync(session.SessionId, "What is the result?", CancellationToken.None);

        response.Reply.Should().Contain("[#LR-1]");
        response.Turns.Should().Contain(t => t.Role == "User");
        response.Turns.Should().Contain(t => t.Role == "Assistant" && t.Content.Contains("[#LR-1]"));
    }

    [Fact]
    public async Task SendUserMessage_executes_parallel_tool_calls_and_preserves_ids()
    {
        var args = JsonSerializer.SerializeToElement(new { });
        var harness = new Harness([
            new LlmResponse(null,
                [
                    new LlmToolCall("call_1", "get_patient_summary", args),
                    new LlmToolCall("call_2", "get_patient_summary", args)
                ],
                new LlmUsage(0, 0),
                LlmFinishReason.ToolCalls),
            new LlmResponse("Reviewed two lookups [#P-1].", [], new LlmUsage(0, 0), LlmFinishReason.Stop)
        ]);
        var session = await harness.Sut.OpenOrResumeSessionAsync(1, 2, CancellationToken.None);

        var response = await harness.Sut.SendUserMessageAsync(session.SessionId, "Summarize", CancellationToken.None);

        response.Reply.Should().Contain("[#P-1]");
        response.Turns.Where(t => t.Role == "Tool").Select(t => t.ToolCallId).Should().Contain(["call_1", "call_2"]);
    }

    [Fact]
    public async Task SendUserMessage_returns_cap_reply_when_tool_budget_is_exceeded()
    {
        var args = JsonSerializer.SerializeToElement(new { });
        var harness = new Harness([
            new LlmResponse(null,
                [
                    new LlmToolCall("call_1", "get_patient_summary", args),
                    new LlmToolCall("call_2", "get_patient_summary", args)
                ],
                new LlmUsage(0, 0),
                LlmFinishReason.ToolCalls)
        ], maxToolCalls: 1);
        var session = await harness.Sut.OpenOrResumeSessionAsync(1, 2, CancellationToken.None);

        var response = await harness.Sut.SendUserMessageAsync(session.SessionId, "Summarize", CancellationToken.None);

        response.Reply.Should().Contain("too many chart lookups");
        response.Turns.Should().NotContain(t => t.Role == "Tool");
    }

    [Fact]
    public async Task OpenOrResumeSession_reuses_fresh_session_and_regenerates_stale_session()
    {
        var harness = new Harness([]);
        var first = await harness.Sut.OpenOrResumeSessionAsync(1, 2, CancellationToken.None);

        harness.Clock.Now = harness.Clock.Now.AddHours(23);
        var fresh = await harness.Sut.OpenOrResumeSessionAsync(1, 2, CancellationToken.None);

        harness.Clock.Now = harness.Clock.Now.AddHours(2);
        var stale = await harness.Sut.OpenOrResumeSessionAsync(1, 2, CancellationToken.None);

        fresh.SessionId.Should().Be(first.SessionId);
        stale.SessionId.Should().NotBe(first.SessionId);
    }

    private sealed class Harness
    {
        public Harness(IReadOnlyList<LlmResponse> responses, int maxToolCalls = 8)
        {
            Clock = new TestClock { Now = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc) };
            var store = new InMemorySinaSessionStore();
            var selector = new TestSelector(responses);
            var tools = new ToolRegistry([new TestTool()]);
            Sut = new SinaManager(
                store,
                new TestSummarizer(),
                new TestAlertEngine(),
                new SinaSafetyFilter(),
                tools,
                selector,
                Clock,
                Options.Create(new SinaLlmConfig { MaxToolCallsPerTurn = maxToolCalls }));
        }

        public SinaManager Sut { get; }
        public TestClock Clock { get; }
    }

    private sealed class TestClock : ISinaClock
    {
        public DateTime Now { get; set; }
        public DateTime UtcNow() => Now;
    }

    private sealed class TestSummarizer : IContextSummarizer
    {
        public Task<string> BuildSystemMessageAsync(int patientId, CancellationToken ct) => Task.FromResult($"Patient [#P-{patientId}].");
    }

    private sealed class TestAlertEngine : IProactiveAlertEngine
    {
        public Task<IReadOnlyList<SinaAlert>> ScanAsync(int patientId, CancellationToken ct) => Task.FromResult<IReadOnlyList<SinaAlert>>([]);
        public string RenderAlerts(IReadOnlyList<SinaAlert> alerts) => "No alerts.";
    }

    private sealed class TestSelector : ILlmProviderSelector
    {
        private readonly TestLlmClient client;
        public TestSelector(IReadOnlyList<LlmResponse> responses) => client = new TestLlmClient(responses);
        public IClinicalLlmClient GetClient() => client;
    }

    private sealed class TestLlmClient : IClinicalLlmClient
    {
        private readonly Queue<LlmResponse> responses;
        public TestLlmClient(IReadOnlyList<LlmResponse> responses) => this.responses = new Queue<LlmResponse>(responses);
        public string ProviderName => "Test";
        public Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct) => Task.FromResult(responses.Dequeue());
    }

    private sealed class TestTool : ISinaTool
    {
        public string Name => "get_patient_summary";
        public string Description => "Test patient summary tool.";
        public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });
        public Task<JsonElement> ExecuteAsync(JsonElement arguments, ToolExecutionContext ctx, CancellationToken ct)
            => Task.FromResult(JsonSerializer.SerializeToElement(new { record_id = $"#P-{ctx.PatientId}" }));
    }

    private sealed class InMemorySinaSessionStore : ISinaSessionStore
    {
        private readonly Dictionary<Guid, MutableSession> sessions = [];

        public Task<SinaSessionDto?> GetActiveSessionAsync(int patientId, int healthCareProviderId, CancellationToken ct)
        {
            var session = sessions.Values
                .Where(s => s.PatientId == patientId && s.HealthCareProviderId == healthCareProviderId && s.Status == SinaSessionStatus.Active)
                .OrderByDescending(s => s.LastInteractionAt)
                .FirstOrDefault();
            return Task.FromResult(session?.ToDto());
        }

        public Task<SinaSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken ct)
        {
            return Task.FromResult(sessions.TryGetValue(sessionId, out var session) ? session.ToDto() : null);
        }

        public Task<SinaSessionDto> CreateSessionAsync(int patientId, int healthCareProviderId, DateTime nowUtc, CancellationToken ct)
        {
            var session = new MutableSession(Guid.NewGuid(), patientId, healthCareProviderId, nowUtc, nowUtc, SinaSessionStatus.Active);
            sessions.Add(session.Id, session);
            return Task.FromResult(session.ToDto());
        }

        public async Task<SinaTurnDto> AppendTurnAsync(Guid sessionId, SinaTurnCreate turn, DateTime nowUtc, CancellationToken ct)
        {
            var turns = await AppendTurnsAsync(sessionId, [turn], nowUtc, ct);
            return turns[0];
        }

        public Task<IReadOnlyList<SinaTurnDto>> AppendTurnsAsync(Guid sessionId, IReadOnlyList<SinaTurnCreate> turns, DateTime nowUtc, CancellationToken ct)
        {
            var session = sessions[sessionId];
            foreach (var turn in turns)
            {
                session.Turns.Add(new SinaTurnDto(Guid.NewGuid(), sessionId, session.Turns.Count, turn.Role, turn.Content, turn.ToolName, turn.ToolCallId, nowUtc));
            }

            return Task.FromResult<IReadOnlyList<SinaTurnDto>>(session.Turns.TakeLast(turns.Count).ToArray());
        }

        public Task TouchAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct)
        {
            sessions[sessionId].LastInteractionAt = nowUtc;
            return Task.CompletedTask;
        }

        public Task CloseSessionAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct)
        {
            if (sessions.TryGetValue(sessionId, out var session))
            {
                session.Status = SinaSessionStatus.Closed;
                session.LastInteractionAt = nowUtc;
            }

            return Task.CompletedTask;
        }

        private sealed class MutableSession
        {
            public MutableSession(Guid id, int patientId, int healthCareProviderId, DateTime startedAt, DateTime lastInteractionAt, SinaSessionStatus status)
            {
                Id = id;
                PatientId = patientId;
                HealthCareProviderId = healthCareProviderId;
                StartedAt = startedAt;
                LastInteractionAt = lastInteractionAt;
                Status = status;
            }

            public Guid Id { get; }
            public int PatientId { get; }
            public int HealthCareProviderId { get; }
            public DateTime StartedAt { get; }
            public DateTime LastInteractionAt { get; set; }
            public SinaSessionStatus Status { get; set; }
            public List<SinaTurnDto> Turns { get; } = [];
            public SinaSessionDto ToDto() => new(Id, PatientId, HealthCareProviderId, StartedAt, LastInteractionAt, Status, Turns.ToArray());
        }
    }
}
