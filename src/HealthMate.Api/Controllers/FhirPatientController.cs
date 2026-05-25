using System.Diagnostics;
using System.Globalization;
using HealthMate.Fhir.Mapping;
using HealthMate.Fhir.Ports;
using HealthMate.Fhir.Search;
using HealthMate.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers;

[Authorize(Policy = "HealthCareProviderOnly")]
[ApiController]
[Route("fhir/Patient")]
[Produces("application/fhir+json")]
public sealed class FhirPatientController(
    IFhirPatientStore store,
    PatientResourceMapper mapper,
    PatientSearchParser searchParser,
    PatientBundleAssembler bundleAssembler,
    OperationOutcomeFactory outcomes,
    FhirJsonService fhirJson,
    ILogger<FhirPatientController> logger) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<IActionResult> Read(string id, CancellationToken ct)
    {
        return Logged("read", id, async () =>
        {
            var snapshot = await store.ReadAsync(id, ct);
            if (snapshot is null)
            {
                return Outcome(StatusCodes.Status404NotFound, outcomes.NotFound("Patient", id));
            }

            if (snapshot.IsDeleted)
            {
                return Outcome(StatusCodes.Status410Gone, outcomes.Gone("Patient", id));
            }

            if (Request.Headers.IfNoneMatch.Any(value => MatchesWeakVersion(value, snapshot.VersionId)))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            SetVersionHeaders(snapshot.VersionId, snapshot.LastUpdated);
            return Fhir(StatusCodes.Status200OK, mapper.ToResource(snapshot));
        });
    }

    [HttpGet]
    public Task<IActionResult> Search(CancellationToken ct)
    {
        return Logged("search", null, async () =>
        {
            var query = searchParser.Parse(Request.Query);
            var result = await store.SearchAsync(query, ct);
            var bundle = bundleAssembler.BuildSearchset(result, CurrentUrl(), ResourceBaseUrl());
            return Fhir(StatusCodes.Status200OK, bundle);
        });
    }

    [HttpPost]
    public Task<IActionResult> Create(CancellationToken ct)
    {
        return Logged("create", null, async () =>
        {
            var patient = await ParsePatientBodyAsync(ct);
            if (!string.IsNullOrWhiteSpace(patient.Id))
            {
                return Outcome(StatusCodes.Status400BadRequest, outcomes.Invalid("Patient.id must be empty on create.", "Patient.id"));
            }

            var created = await store.CreateAsync(mapper.ToSnapshot(patient), ct);
            Response.Headers.Location = $"/fhir/Patient/{created.FhirId}";
            SetVersionHeaders(created.VersionId, created.LastUpdated);
            return Fhir(StatusCodes.Status201Created, mapper.ToResource(created));
        });
    }

    [HttpPut("{id}")]
    public Task<IActionResult> Update(string id, CancellationToken ct)
    {
        return Logged("update", id, async () =>
        {
            if (!TryReadIfMatch(out var expectedVersion))
            {
                throw new FhirPreconditionRequiredException("PUT /fhir/Patient/{id} requires If-Match: W/\"{versionId}\".");
            }

            var patient = await ParsePatientBodyAsync(ct);
            if (!string.Equals(patient.Id, id, StringComparison.Ordinal))
            {
                return Outcome(StatusCodes.Status400BadRequest, outcomes.Invalid("Patient.id must match the URL id on update.", "Patient.id"));
            }

            var updated = await store.UpdateAsync(mapper.ToSnapshot(patient), expectedVersion, ct);
            SetVersionHeaders(updated.VersionId, updated.LastUpdated);
            return Fhir(StatusCodes.Status200OK, mapper.ToResource(updated));
        });
    }

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        return Logged("delete", id, async () =>
        {
            uint? expectedVersion = null;
            if (Request.Headers.IfMatch.Count > 0)
            {
                if (!TryParseWeakVersion(Request.Headers.IfMatch.FirstOrDefault(), out var parsed))
                {
                    return Outcome(StatusCodes.Status400BadRequest, outcomes.Invalid("If-Match must be a weak ETag like W/\"1\"."));
                }

                expectedVersion = parsed;
            }

            await store.DeleteAsync(id, expectedVersion, ct);
            return NoContent();
        });
    }

    [HttpGet("{id}/_history")]
    public Task<IActionResult> History(string id, CancellationToken ct)
    {
        return Logged("history", id, async () =>
        {
            var count = ReadHistoryCount();
            var since = ReadSince();
            var history = await store.ReadHistoryAsync(id, count, since, ct);
            if (history.Total == 0 && await store.ReadAsync(id, ct) is null)
            {
                return Outcome(StatusCodes.Status404NotFound, outcomes.NotFound("Patient", id));
            }

            return Fhir(StatusCodes.Status200OK, bundleAssembler.BuildHistory(history, CurrentUrl(), ResourceBaseUrl()));
        });
    }

    [HttpGet("{id}/_history/{vid}")]
    public Task<IActionResult> VRead(string id, string vid, CancellationToken ct)
    {
        return Logged("vread", id, async () =>
        {
            if (!uint.TryParse(vid, NumberStyles.None, CultureInfo.InvariantCulture, out var versionId))
            {
                return Outcome(StatusCodes.Status400BadRequest, outcomes.Invalid("Version id must be an unsigned integer.", "vid"));
            }

            var entry = await store.ReadVersionAsync(id, versionId, ct);
            if (entry is null)
            {
                return Outcome(StatusCodes.Status404NotFound, outcomes.NotFound("Patient", $"{id}/_history/{vid}"));
            }

            SetVersionHeaders(entry.Snapshot.VersionId, entry.Snapshot.LastUpdated);
            return Fhir(StatusCodes.Status200OK, mapper.ToResource(entry.Snapshot));
        });
    }

    [HttpPost("$validate")]
    public async Task<IActionResult> Validate(CancellationToken ct)
    {
        var body = await ReadBodyAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Outcome(StatusCodes.Status200OK, outcomes.Invalid("FHIR Patient body is required."));
        }

        try
        {
            var patient = fhirJson.Parse<Patient>(body);
            mapper.ToSnapshot(patient);
            return Outcome(StatusCodes.Status200OK, outcomes.Valid());
        }
        catch (FhirValidationException ex)
        {
            return Outcome(StatusCodes.Status200OK, outcomes.Invalid(ex.Issues));
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return Outcome(StatusCodes.Status200OK, outcomes.Invalid("Malformed FHIR Patient JSON."));
        }
    }

    private async Task<Patient> ParsePatientBodyAsync(CancellationToken ct)
    {
        var body = await ReadBodyAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new FhirValidationException("FHIR Patient body is required.");
        }

        try
        {
            return fhirJson.Parse<Patient>(body);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            throw new FhirValidationException("Malformed FHIR Patient JSON.");
        }
    }

    private async Task<string> ReadBodyAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync(ct);
    }

    private IActionResult Fhir(int statusCode, Resource resource)
    {
        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/fhir+json",
            Content = fhirJson.Serialize(resource)
        };
    }

    private IActionResult Outcome(int statusCode, OperationOutcome outcome) => Fhir(statusCode, outcome);

    private void SetVersionHeaders(uint versionId, DateTimeOffset lastUpdated)
    {
        Response.Headers.ETag = $"W/\"{versionId}\"";
        Response.Headers.LastModified = lastUpdated.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
    }

    private bool TryReadIfMatch(out uint versionId)
    {
        versionId = 0;
        if (Request.Headers.IfMatch.Count == 0)
        {
            return false;
        }

        if (TryParseWeakVersion(Request.Headers.IfMatch.FirstOrDefault(), out versionId))
        {
            return true;
        }

        throw new FhirValidationException("If-Match must be a weak ETag like W/\"1\".");
    }

    private static bool MatchesWeakVersion(string? candidate, uint versionId)
    {
        return TryParseWeakVersion(candidate, out var parsed) && parsed == versionId;
    }

    private static bool TryParseWeakVersion(string? value, out uint versionId)
    {
        versionId = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("W/\"", StringComparison.Ordinal) && trimmed.EndsWith('"'))
        {
            return uint.TryParse(trimmed[3..^1], NumberStyles.None, CultureInfo.InvariantCulture, out versionId);
        }

        if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
        {
            return uint.TryParse(trimmed[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out versionId);
        }

        return uint.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out versionId);
    }

    private int ReadHistoryCount()
    {
        if (!Request.Query.TryGetValue("_count", out var values) || !int.TryParse(values.FirstOrDefault(), out var count))
        {
            return 50;
        }

        return Math.Clamp(count, 1, 100);
    }

    private DateTimeOffset? ReadSince()
    {
        if (!Request.Query.TryGetValue("_since", out var values) || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(values.FirstOrDefault(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            throw new FhirValidationException("_since must be a valid FHIR instant.", "_since");
        }

        return parsed.ToUniversalTime();
    }

    private string CurrentUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";
    }

    private string ResourceBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/fhir/Patient";
    }

    private async Task<IActionResult> Logged(string operation, string? patientId, Func<Task<IActionResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            var result = await action();
            success = StatusCodeFrom(result) < 400;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "FHIR request completed {patientId} {hcpId} {operation} {resourceType} {latencyMs} {success}",
                patientId ?? string.Empty,
                ReadHealthCareProviderId(),
                operation,
                "Patient",
                stopwatch.ElapsedMilliseconds,
                success);
        }
    }

    private int ReadHealthCareProviderId()
    {
        return int.TryParse(User.FindFirst("HealthCareProviderId")?.Value, out var hcpId) ? hcpId : 0;
    }

    private static int StatusCodeFrom(IActionResult result)
    {
        return result switch
        {
            ContentResult content => content.StatusCode ?? StatusCodes.Status200OK,
            ObjectResult obj => obj.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult status => status.StatusCode,
            EmptyResult => StatusCodes.Status200OK,
            _ => StatusCodes.Status200OK
        };
    }
}
