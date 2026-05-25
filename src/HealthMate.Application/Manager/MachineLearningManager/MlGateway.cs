using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Manager.MachineLearningManager;

public class MlGateway : IMlGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<MlGateway> _logger;

    public MlGateway(HttpClient httpClient, ILogger<MlGateway> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AnemiaGatewayResponse> PredictAnemiaAsync(
        AnemiaGatewayRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var sw = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "/v1/predict/anemia",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Never log the request body - it carries clinical feature values.
            _logger.LogWarning(
                ex,
                "ML gateway request failed correlationId={CorrelationId} latencyMs={LatencyMs}",
                correlationId,
                sw.ElapsedMilliseconds);
            throw new MlGatewayException("ML service is unreachable.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "ML gateway non-2xx correlationId={CorrelationId} status={Status} latencyMs={LatencyMs}",
                correlationId,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);
            throw new MlGatewayException(
                $"ML service returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        AnemiaGatewayResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<AnemiaGatewayResponse>(
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "ML gateway returned malformed JSON correlationId={CorrelationId}",
                correlationId);
            throw new MlGatewayException("ML service returned malformed JSON.", ex);
        }

        if (payload is null)
        {
            throw new MlGatewayException("ML service returned an empty response body.");
        }

        _logger.LogInformation(
            "ML gateway ok correlationId={CorrelationId} latencyMs={LatencyMs} model={ModelName}/{ModelVersion}",
            correlationId,
            sw.ElapsedMilliseconds,
            payload.ModelName,
            payload.ModelVersion);

        return payload;
    }
}
