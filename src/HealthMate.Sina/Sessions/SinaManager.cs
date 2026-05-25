using System.Net;
using System.Text.Json;
using HealthMate.Sina.Llm;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Tools;
using Microsoft.Extensions.Options;

namespace HealthMate.Sina.Sessions;

public class SinaManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ISinaSessionStore sessionStore;
    private readonly IContextSummarizer contextSummarizer;
    private readonly IProactiveAlertEngine alertEngine;
    private readonly ISinaSafetyFilter safetyFilter;
    private readonly ToolRegistry toolRegistry;
    private readonly ILlmProviderSelector providerSelector;
    private readonly ISinaClock clock;
    private readonly SinaLlmConfig config;

    public SinaManager(
        ISinaSessionStore sessionStore,
        IContextSummarizer contextSummarizer,
        IProactiveAlertEngine alertEngine,
        ISinaSafetyFilter safetyFilter,
        ToolRegistry toolRegistry,
        ILlmProviderSelector providerSelector,
        ISinaClock clock,
        IOptions<SinaLlmConfig> options)
    {
        this.sessionStore = sessionStore;
        this.contextSummarizer = contextSummarizer;
        this.alertEngine = alertEngine;
        this.safetyFilter = safetyFilter;
        this.toolRegistry = toolRegistry;
        this.providerSelector = providerSelector;
        this.clock = clock;
        config = options.Value;
    }

    public async Task<OpenSessionResponse> OpenOrResumeSessionAsync(int patientId, int healthCareProviderId, CancellationToken ct)
    {
        var now = clock.UtcNow();
        var active = await sessionStore.GetActiveSessionAsync(patientId, healthCareProviderId, ct);
        if (active is not null && active.LastInteractionAt > now.AddHours(-24))
        {
            var currentAlerts = await alertEngine.ScanAsync(patientId, ct);
            return new OpenSessionResponse(active.Id, active.Turns.Select(ToView).ToArray(), currentAlerts);
        }

        if (active is not null)
        {
            await sessionStore.CloseSessionAsync(active.Id, now, ct);
        }

        var session = await sessionStore.CreateSessionAsync(patientId, healthCareProviderId, now, ct);
        var systemMessage = await contextSummarizer.BuildSystemMessageAsync(patientId, ct);
        var alerts = await alertEngine.ScanAsync(patientId, ct);
        var alertMessage = alertEngine.RenderAlerts(alerts);

        await sessionStore.AppendTurnsAsync(session.Id,
            [
                new SinaTurnCreate(SinaTurnRole.System, SerializeText(systemMessage)),
                new SinaTurnCreate(SinaTurnRole.System, SerializeText(alertMessage))
            ],
            now,
            ct);

        var created = await sessionStore.GetSessionAsync(session.Id, ct) ?? session;
        return new OpenSessionResponse(created.Id, created.Turns.Select(ToView).ToArray(), alerts);
    }

    public async Task<SinaTurnResponse> SendUserMessageAsync(Guid sessionId, string userMessage, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("Message content is required.", nameof(userMessage));
        }

        var session = await sessionStore.GetSessionAsync(sessionId, ct)
            ?? throw new InvalidOperationException("Sina session was not found.");
        if (session.Status != SinaSessionStatus.Active)
        {
            throw new InvalidOperationException("Sina session is closed.");
        }

        var now = clock.UtcNow();
        await sessionStore.AppendTurnAsync(sessionId, new SinaTurnCreate(SinaTurnRole.User, SerializeText(userMessage)), now, ct);

        if (safetyFilter.TryBuildNonClinicalResponse(userMessage, out var refusal))
        {
            var assistant = await sessionStore.AppendTurnAsync(sessionId, new SinaTurnCreate(SinaTurnRole.Assistant, SerializeText(refusal)), now, ct);
            await sessionStore.TouchAsync(sessionId, now, ct);
            var finalSession = await sessionStore.GetSessionAsync(sessionId, ct) ?? session;
            return new SinaTurnResponse(refusal, finalSession.Turns.Select(ToView).ToArray(), safetyFilter.ExtractCitations(refusal));
        }

        var executedToolCalls = 0;
        while (true)
        {
            session = await sessionStore.GetSessionAsync(sessionId, ct)
                ?? throw new InvalidOperationException("Sina session was not found.");
            var response = await GenerateAsync(BuildLlmRequest(session), ct);

            if (response.ToolCalls.Count == 0)
            {
                var reply = safetyFilter.ApplyAssistantGuards(userMessage, response.Text ?? string.Empty);
                await sessionStore.AppendTurnAsync(sessionId, new SinaTurnCreate(SinaTurnRole.Assistant, SerializeText(reply)), clock.UtcNow(), ct);
                await sessionStore.TouchAsync(sessionId, clock.UtcNow(), ct);
                var finalSession = await sessionStore.GetSessionAsync(sessionId, ct) ?? session;
                return new SinaTurnResponse(reply, finalSession.Turns.Select(ToView).ToArray(), safetyFilter.ExtractCitations(reply));
            }

            if (executedToolCalls + response.ToolCalls.Count > config.MaxToolCallsPerTurn)
            {
                const string capReply = "I needed too many chart lookups to answer safely. Please narrow the question.";
                await sessionStore.AppendTurnAsync(sessionId, new SinaTurnCreate(SinaTurnRole.Assistant, SerializeText(capReply)), clock.UtcNow(), ct);
                await sessionStore.TouchAsync(sessionId, clock.UtcNow(), ct);
                var cappedSession = await sessionStore.GetSessionAsync(sessionId, ct) ?? session;
                return new SinaTurnResponse(capReply, cappedSession.Turns.Select(ToView).ToArray(), []);
            }

            await sessionStore.AppendTurnAsync(
                sessionId,
                new SinaTurnCreate(
                    SinaTurnRole.Assistant,
                    JsonSerializer.Serialize(new AssistantToolTurnPayload(response.Text, response.ToolCalls), JsonOptions),
                    response.ToolCalls.Count == 1 ? response.ToolCalls[0].Name : null,
                    response.ToolCalls.Count == 1 ? response.ToolCalls[0].Id : null),
                clock.UtcNow(),
                ct);

            var ctx = new ToolExecutionContext(session.PatientId, session.HealthCareProviderId);
            var toolTurns = new List<SinaTurnCreate>();
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await toolRegistry.ExecuteAsync(toolCall.Name, toolCall.Arguments, ctx, ct);
                toolTurns.Add(new SinaTurnCreate(SinaTurnRole.Tool, result.GetRawText(), toolCall.Name, toolCall.Id));
            }

            await sessionStore.AppendTurnsAsync(sessionId, toolTurns, clock.UtcNow(), ct);
            executedToolCalls += response.ToolCalls.Count;
        }
    }

    public Task CloseSessionAsync(Guid sessionId, CancellationToken ct)
    {
        return sessionStore.CloseSessionAsync(sessionId, clock.UtcNow(), ct);
    }

    private async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        try
        {
            return await providerSelector.GetClient().GenerateAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            throw new SinaUnavailableException("Sina is unavailable; please consult the chart directly.", ex);
        }
    }

    private LlmRequest BuildLlmRequest(SinaSessionDto session)
    {
        var systemInstruction = string.Join(Environment.NewLine + Environment.NewLine,
            session.Turns.Where(t => t.Role == SinaTurnRole.System).OrderBy(t => t.OrdinalIndex).Select(t => ExtractText(t.Content)));

        var messages = session.Turns
            .Where(t => t.Role != SinaTurnRole.System)
            .OrderBy(t => t.OrdinalIndex)
            .Select(ToLlmMessage)
            .ToArray();

        return new LlmRequest(systemInstruction, messages, toolRegistry.GetSchemas());
    }

    private static LlmMessage ToLlmMessage(SinaTurnDto turn)
    {
        return turn.Role switch
        {
            SinaTurnRole.Assistant when TryDeserializeAssistantToolPayload(turn.Content, out var payload) =>
                new LlmMessage(LlmRole.Assistant, payload.Text, payload.ToolCalls, null, null),
            SinaTurnRole.Assistant => new LlmMessage(LlmRole.Assistant, ExtractText(turn.Content), null, null, null),
            SinaTurnRole.Tool => new LlmMessage(LlmRole.Tool, turn.Content, null, turn.ToolCallId, turn.ToolName),
            _ => new LlmMessage(LlmRole.User, ExtractText(turn.Content), null, null, null)
        };
    }

    private static SinaTurnView ToView(SinaTurnDto turn)
    {
        var content = turn.Role == SinaTurnRole.Tool ? turn.Content : ExtractText(turn.Content);
        if (turn.Role == SinaTurnRole.Assistant && string.IsNullOrWhiteSpace(content) && TryDeserializeAssistantToolPayload(turn.Content, out var payload))
        {
            content = "Requested tools: " + string.Join(", ", payload.ToolCalls.Select(c => c.Name));
        }

        return new SinaTurnView(turn.Id, turn.Role.ToString(), content, turn.CreatedAt, turn.ToolName, turn.ToolCallId);
    }

    private static string SerializeText(string text)
    {
        return JsonSerializer.Serialize(new TextTurnPayload(text), JsonOptions);
    }

    private static string ExtractText(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("text", out var text))
            {
                return text.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return content;
    }

    private static bool TryDeserializeAssistantToolPayload(string content, out AssistantToolTurnPayload payload)
    {
        try
        {
            payload = JsonSerializer.Deserialize<AssistantToolTurnPayload>(content, JsonOptions) ?? new AssistantToolTurnPayload(null, []);
            return payload.ToolCalls.Count > 0;
        }
        catch (JsonException)
        {
            payload = new AssistantToolTurnPayload(null, []);
            return false;
        }
    }

    private sealed record TextTurnPayload(string Text);

    private sealed record AssistantToolTurnPayload(string? Text, IReadOnlyList<LlmToolCall> ToolCalls);
}
