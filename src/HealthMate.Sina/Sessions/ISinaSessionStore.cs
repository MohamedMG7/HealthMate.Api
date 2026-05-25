namespace HealthMate.Sina.Sessions;

public interface ISinaSessionStore
{
    Task<SinaSessionDto?> GetActiveSessionAsync(int patientId, int healthCareProviderId, CancellationToken ct);
    Task<SinaSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken ct);
    Task<SinaSessionDto> CreateSessionAsync(int patientId, int healthCareProviderId, DateTime nowUtc, CancellationToken ct);
    Task<SinaTurnDto> AppendTurnAsync(Guid sessionId, SinaTurnCreate turn, DateTime nowUtc, CancellationToken ct);
    Task<IReadOnlyList<SinaTurnDto>> AppendTurnsAsync(Guid sessionId, IReadOnlyList<SinaTurnCreate> turns, DateTime nowUtc, CancellationToken ct);
    Task TouchAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct);
    Task CloseSessionAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct);
}
