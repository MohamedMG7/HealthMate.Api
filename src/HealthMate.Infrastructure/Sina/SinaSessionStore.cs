using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using SinaSessionDto = HealthMate.Sina.Sessions.SinaSessionDto;
using SinaTurnDto = HealthMate.Sina.Sessions.SinaTurnDto;
using SinaTurnCreate = HealthMate.Sina.Sessions.SinaTurnCreate;
using PortSessionStatus = HealthMate.Sina.Sessions.SinaSessionStatus;
using PortTurnRole = HealthMate.Sina.Sessions.SinaTurnRole;
using InfraSessionStatus = HealthMate.Infrastructure.Enums.SinaSessionStatus;
using InfraTurnRole = HealthMate.Infrastructure.Enums.SinaTurnRole;

namespace HealthMate.Infrastructure.Sina;

public class SinaSessionStore : HealthMate.Sina.Sessions.ISinaSessionStore
{
    private readonly HealthMateContext context;

    public SinaSessionStore(HealthMateContext context)
    {
        this.context = context;
    }

    public async Task<SinaSessionDto?> GetActiveSessionAsync(int patientId, int healthCareProviderId, CancellationToken ct)
    {
        var session = await context.SinaSessions
            .AsNoTracking()
            .Include(s => s.Turns)
            .Where(s => s.PatientId == patientId && s.HealthCareProviderId == healthCareProviderId && s.Status == InfraSessionStatus.Active)
            .OrderByDescending(s => s.LastInteractionAt)
            .FirstOrDefaultAsync(ct);

        return session is null ? null : Map(session);
    }

    public async Task<SinaSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await context.SinaSessions
            .AsNoTracking()
            .Include(s => s.Turns)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        return session is null ? null : Map(session);
    }

    public async Task<SinaSessionDto> CreateSessionAsync(int patientId, int healthCareProviderId, DateTime nowUtc, CancellationToken ct)
    {
        var session = new SinaSession
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            HealthCareProviderId = healthCareProviderId,
            StartedAt = nowUtc,
            LastInteractionAt = nowUtc,
            Status = InfraSessionStatus.Active
        };

        context.SinaSessions.Add(session);
        await context.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<SinaTurnDto> AppendTurnAsync(Guid sessionId, SinaTurnCreate turn, DateTime nowUtc, CancellationToken ct)
    {
        var appended = await AppendTurnsAsync(sessionId, [turn], nowUtc, ct);
        return appended[0];
    }

    public async Task<IReadOnlyList<SinaTurnDto>> AppendTurnsAsync(Guid sessionId, IReadOnlyList<SinaTurnCreate> turns, DateTime nowUtc, CancellationToken ct)
    {
        if (turns.Count == 0)
        {
            return [];
        }

        var exists = await context.SinaSessions.AnyAsync(s => s.Id == sessionId, ct);
        if (!exists)
        {
            throw new InvalidOperationException("Sina session was not found.");
        }

        var maxOrdinal = await context.SinaTurns
            .Where(t => t.SessionId == sessionId)
            .Select(t => (int?)t.OrdinalIndex)
            .MaxAsync(ct) ?? -1;

        var entities = turns.Select((turn, index) => new SinaTurn
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            OrdinalIndex = maxOrdinal + index + 1,
            Role = Map(turn.Role),
            Content = turn.Content,
            ToolName = turn.ToolName,
            ToolCallId = turn.ToolCallId,
            CreatedAt = nowUtc
        }).ToArray();

        context.SinaTurns.AddRange(entities);
        await context.SaveChangesAsync(ct);
        return entities.Select(Map).ToArray();
    }

    public async Task TouchAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct)
    {
        var session = await context.SinaSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
        {
            return;
        }

        session.LastInteractionAt = nowUtc;
        await context.SaveChangesAsync(ct);
    }

    public async Task CloseSessionAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct)
    {
        var session = await context.SinaSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
        {
            return;
        }

        session.Status = InfraSessionStatus.Closed;
        session.LastInteractionAt = nowUtc;
        await context.SaveChangesAsync(ct);
    }

    private static SinaSessionDto Map(SinaSession session)
    {
        return new SinaSessionDto(
            session.Id,
            session.PatientId,
            session.HealthCareProviderId,
            session.StartedAt,
            session.LastInteractionAt,
            Map(session.Status),
            session.Turns.OrderBy(t => t.OrdinalIndex).Select(Map).ToArray());
    }

    private static SinaTurnDto Map(SinaTurn turn)
    {
        return new SinaTurnDto(turn.Id, turn.SessionId, turn.OrdinalIndex, Map(turn.Role), turn.Content, turn.ToolName, turn.ToolCallId, turn.CreatedAt);
    }

    private static PortSessionStatus Map(InfraSessionStatus status) => status == InfraSessionStatus.Active ? PortSessionStatus.Active : PortSessionStatus.Closed;
    private static InfraSessionStatus Map(PortSessionStatus status) => status == PortSessionStatus.Active ? InfraSessionStatus.Active : InfraSessionStatus.Closed;

    private static PortTurnRole Map(InfraTurnRole role) => role switch
    {
        InfraTurnRole.System => PortTurnRole.System,
        InfraTurnRole.User => PortTurnRole.User,
        InfraTurnRole.Assistant => PortTurnRole.Assistant,
        InfraTurnRole.Tool => PortTurnRole.Tool,
        _ => PortTurnRole.User
    };

    private static InfraTurnRole Map(PortTurnRole role) => role switch
    {
        PortTurnRole.System => InfraTurnRole.System,
        PortTurnRole.User => InfraTurnRole.User,
        PortTurnRole.Assistant => InfraTurnRole.Assistant,
        PortTurnRole.Tool => InfraTurnRole.Tool,
        _ => InfraTurnRole.User
    };
}
