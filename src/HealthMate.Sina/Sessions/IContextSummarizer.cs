namespace HealthMate.Sina.Sessions;

public interface IContextSummarizer
{
    Task<string> BuildSystemMessageAsync(int patientId, CancellationToken ct);
}
