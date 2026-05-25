namespace HealthMate.Application.Manager.MachineLearningManager;

public interface IMlGateway
{
    Task<AnemiaGatewayResponse> PredictAnemiaAsync(AnemiaGatewayRequest request, CancellationToken cancellationToken);
}

// IMPORTANT: no PatientId on this record. The ML service receives only the
// feature vector. PHI minimization at the wire format (CONSTITUTION).
public record AnemiaGatewayRequest(
    decimal Hb,
    decimal Rbc,
    decimal Pcv,
    decimal Mch,
    decimal Mchc);

public record AnemiaGatewayResponse(
    bool Anemia,
    double? Confidence,
    string ModelName,
    string ModelVersion,
    DateTimeOffset PredictedAt);

public class MlGatewayException : Exception
{
    public MlGatewayException(string message) : base(message) { }
    public MlGatewayException(string message, Exception inner) : base(message, inner) { }
}

public class NoCbcDataException : Exception
{
    public int PatientId { get; }

    public NoCbcDataException(int patientId)
        : base($"No recent CBC test on file for patient {patientId}.")
    {
        PatientId = patientId;
    }
}
