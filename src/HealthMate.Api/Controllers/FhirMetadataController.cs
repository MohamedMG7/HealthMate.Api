using HealthMate.Fhir.Mapping;
using HealthMate.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers;

[ApiController]
[Route("fhir/metadata")]
public sealed class FhirMetadataController(
    CapabilityStatementFactory capabilityStatementFactory,
    FhirJsonService fhirJson) : ControllerBase
{
    [HttpGet]
    [Produces("application/fhir+json")]
    public IActionResult Get()
    {
        return new ContentResult
        {
            StatusCode = StatusCodes.Status200OK,
            ContentType = "application/fhir+json",
            Content = fhirJson.Serialize(capabilityStatementFactory.Build())
        };
    }
}
