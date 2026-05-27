using FluentAssertions;
using HealthMate.Application.Manager.MachineLearningManager;
using HealthMate.Application.Ml.Contracts;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using Moq;

namespace HealthMate.Tests.BLL.Manager;

public sealed class MachineLearningManagerTests
{
    private const int PatientId = 42;

    [Fact]
    public async Task CheckAnimea_throws_NoCbcDataException_when_all_features_are_zero_and_never_calls_gateway()
    {
        var emptyCbc = new AnimeaMLDto { patientId = PatientId };
        var observationRepo = new Mock<IObservationRepo>();
        observationRepo.Setup(r => r.GetRecentCBCTestForML(PatientId)).ReturnsAsync(emptyCbc);

        var gateway = new Mock<IMlGateway>(MockBehavior.Strict);

        var sut = new MachineLearningManager(observationRepo.Object, gateway.Object);

        await sut
            .Invoking(s => s.CheckAnimea(PatientId))
            .Should()
            .ThrowAsync<NoCbcDataException>();

        gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckAnimea_returns_response_when_gateway_succeeds()
    {
        var cbc = new AnimeaMLDto
        {
            patientId = PatientId,
            Hemoglobin = 7.0m,
            RedBloodCells = 3.0m,
            PackedCellVolume = 22.0m,
            MeanCorpuscularHemoglobin = 20.0m,
            MeanCorpuscularHemoglobinConcentration = 28.0m,
        };
        var observationRepo = new Mock<IObservationRepo>();
        observationRepo.Setup(r => r.GetRecentCBCTestForML(PatientId)).ReturnsAsync(cbc);

        var gatewayResponse = new AnemiaGatewayResponse(
            Anemia: true,
            Confidence: 0.95,
            ModelName: "anemia",
            ModelVersion: "v0",
            PredictedAt: DateTimeOffset.UtcNow);

        var gateway = new Mock<IMlGateway>();
        gateway
            .Setup(g => g.PredictAnemiaAsync(It.IsAny<AnemiaGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gatewayResponse);

        var sut = new MachineLearningManager(observationRepo.Object, gateway.Object);

        var result = await sut.CheckAnimea(PatientId);

        result.Animea.Should().BeTrue();
        result.Confidence.Should().Be(0.95);
        result.ModelVersion.Should().Be("v0");
    }

    [Fact]
    public async Task CheckAnimea_strips_patientId_when_calling_gateway()
    {
        var cbc = new AnimeaMLDto
        {
            patientId = PatientId,
            Hemoglobin = 12.0m,
            RedBloodCells = 4.5m,
            PackedCellVolume = 38.0m,
            MeanCorpuscularHemoglobin = 28.0m,
            MeanCorpuscularHemoglobinConcentration = 32.0m,
        };
        var observationRepo = new Mock<IObservationRepo>();
        observationRepo.Setup(r => r.GetRecentCBCTestForML(PatientId)).ReturnsAsync(cbc);

        AnemiaGatewayRequest? capturedRequest = null;
        var gateway = new Mock<IMlGateway>();
        gateway
            .Setup(g => g.PredictAnemiaAsync(It.IsAny<AnemiaGatewayRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AnemiaGatewayRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new AnemiaGatewayResponse(false, 0.1, "anemia", "v0", DateTimeOffset.UtcNow));

        var sut = new MachineLearningManager(observationRepo.Object, gateway.Object);

        await sut.CheckAnimea(PatientId);

        // PHI minimization: the gateway record must have no patient id.
        // Confirmed at compile time by AnemiaGatewayRequest having no PatientId field;
        // this test guards the manager from leaking it via a future field addition.
        capturedRequest.Should().NotBeNull();
        typeof(AnemiaGatewayRequest).GetProperties()
            .Select(p => p.Name)
            .Should()
            .NotContain(["PatientId", "patientId"]);
    }

    [Fact]
    public async Task CheckAnimea_propagates_MlGatewayException_instead_of_returning_false()
    {
        // This is the critical safety test: the legacy code silently returned
        // Animea=false on any gateway failure, which is medically dangerous.
        // The new manager must surface the failure, not swallow it.
        var cbc = new AnimeaMLDto
        {
            patientId = PatientId,
            Hemoglobin = 5.0m,
            RedBloodCells = 3.0m,
            PackedCellVolume = 20.0m,
            MeanCorpuscularHemoglobin = 18.0m,
            MeanCorpuscularHemoglobinConcentration = 28.0m,
        };
        var observationRepo = new Mock<IObservationRepo>();
        observationRepo.Setup(r => r.GetRecentCBCTestForML(PatientId)).ReturnsAsync(cbc);

        var gateway = new Mock<IMlGateway>();
        gateway
            .Setup(g => g.PredictAnemiaAsync(It.IsAny<AnemiaGatewayRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MlGatewayException("ML service is unreachable."));

        var sut = new MachineLearningManager(observationRepo.Object, gateway.Object);

        await sut
            .Invoking(s => s.CheckAnimea(PatientId))
            .Should()
            .ThrowAsync<MlGatewayException>();
    }
}
