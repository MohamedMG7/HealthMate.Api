namespace HealthMate.Sina.Sessions;

public interface IProactiveAlertEngine
{
    Task<IReadOnlyList<SinaAlert>> ScanAsync(int patientId, CancellationToken ct);
    string RenderAlerts(IReadOnlyList<SinaAlert> alerts);
}
