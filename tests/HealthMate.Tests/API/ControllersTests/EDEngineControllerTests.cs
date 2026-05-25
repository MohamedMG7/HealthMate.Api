using System.Security.Claims;
using FluentAssertions;
using HealthMate.Api.Controllers;
using HealthMate.Application.Manager.MachineLearningManager;
using HealthMate.Infrastructure.DTO.MachineLearningDto;
using HealthMate.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HealthMate.Tests.API.ControllersTests;

public sealed class EDEngineControllerTests
{
    private const int PatientId = 7;

    private static EDEngineController BuildController(
        IMachineLearningManager mlManager,
        IValidationRepo validationRepo,
        string? patientIdClaim = "7")
    {
        var controller = new EDEngineController(validationRepo, mlManager, NullLogger<EDEngineController>.Instance);

        var claims = new List<Claim>();
        if (patientIdClaim is not null)
        {
            claims.Add(new Claim("PatientId", patientIdClaim));
        }
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
        return controller;
    }

    [Fact]
    public async Task Check_returns_400_when_CBC_is_missing()
    {
        var mlManager = new Mock<IMachineLearningManager>();
        mlManager
            .Setup(m => m.CheckAnimea(PatientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoCbcDataException(PatientId));

        var validationRepo = new Mock<IValidationRepo>();
        validationRepo.Setup(v => v.CheckPatientId(PatientId)).ReturnsAsync(true);

        var controller = BuildController(mlManager.Object, validationRepo.Object);

        var result = await controller.Check(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Check_returns_503_when_ML_service_is_unreachable()
    {
        var mlManager = new Mock<IMachineLearningManager>();
        mlManager
            .Setup(m => m.CheckAnimea(PatientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MlGatewayException("ML service is unreachable."));

        var validationRepo = new Mock<IValidationRepo>();
        validationRepo.Setup(v => v.CheckPatientId(PatientId)).ReturnsAsync(true);

        var controller = BuildController(mlManager.Object, validationRepo.Object);

        var result = await controller.Check(CancellationToken.None);

        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Check_returns_200_with_response_on_success()
    {
        var expected = new MachineLearningResponse
        {
            Animea = true,
            Confidence = 0.91,
            ModelVersion = "v0",
        };
        var mlManager = new Mock<IMachineLearningManager>();
        mlManager
            .Setup(m => m.CheckAnimea(PatientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var validationRepo = new Mock<IValidationRepo>();
        validationRepo.Setup(v => v.CheckPatientId(PatientId)).ReturnsAsync(true);

        var controller = BuildController(mlManager.Object, validationRepo.Object);

        var result = await controller.Check(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Check_returns_401_when_PatientId_claim_is_missing()
    {
        var mlManager = new Mock<IMachineLearningManager>(MockBehavior.Strict);
        var validationRepo = new Mock<IValidationRepo>(MockBehavior.Strict);

        var controller = BuildController(mlManager.Object, validationRepo.Object, patientIdClaim: null);

        var result = await controller.Check(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        mlManager.VerifyNoOtherCalls();
        validationRepo.VerifyNoOtherCalls();
    }
}
