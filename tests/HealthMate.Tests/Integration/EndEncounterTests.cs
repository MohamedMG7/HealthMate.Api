using System.Net;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthMate.Tests.Integration;

public sealed class EndEncounterTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task End_encounter_returns_ok_and_persists_finished_encounter()
    {
        var seeded = await EncounterIntegrationTestHelpers.SeedActiveEncounterAsync(fixture.Factory.Services, "end-encounter-happy");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, seeded.ProviderId);

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/end",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                treatmentPlan = "Synthetic treatment plan",
                note = "Synthetic note"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await EncounterIntegrationTestHelpers.ReadJsonAsync<EndEncounterResponse>(response);
        result.EncounterId.Should().Be(seeded.EncounterId);
        result.Status.Should().Be(EncounterStatus.Finished);

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var row = await context.Encounters.SingleAsync(encounter => encounter.Id == seeded.EncounterId);
        row.Status.Should().Be(EncounterStatus.Finished);
        row.EndDate.Should().BeAfter(row.StartDate);
        row.TreatmentPlan.Should().Be("Synthetic treatment plan");
        row.Note.Should().Be("Synthetic note");
        result.EndDate.Should().BeCloseTo(row.EndDate, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task End_encounter_returns_not_found_for_missing_encounter()
    {
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, 123);

        var response = await client.PostAsync(
            "/api/Encounter/999999/end",
            ValidEndBody());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("encounter_not_found");
    }

    [Fact]
    public async Task End_encounter_returns_conflict_when_already_ended()
    {
        var seeded = await EncounterIntegrationTestHelpers.SeedActiveEncounterAsync(fixture.Factory.Services, "end-encounter-conflict");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, seeded.ProviderId);

        var first = await client.PostAsync($"/api/Encounter/{seeded.EncounterId}/end", ValidEndBody());
        var second = await client.PostAsync($"/api/Encounter/{seeded.EncounterId}/end", ValidEndBody());

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var document = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("encounter_already_ended");
    }

    [Fact]
    public async Task End_encounter_rejects_whitespace_treatment_plan()
    {
        var seeded = await EncounterIntegrationTestHelpers.SeedActiveEncounterAsync(fixture.Factory.Services, "end-encounter-validation");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, seeded.ProviderId);

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/end",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                treatmentPlan = "   ",
                note = (string?)null
            }));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().BeOneOf("validation_failed", "domain_rule_failed");
    }

    private static StringContent ValidEndBody()
    {
        return EncounterIntegrationTestHelpers.JsonContent(new
        {
            treatmentPlan = "Synthetic treatment plan",
            note = (string?)null
        });
    }

    private sealed record EndEncounterResponse(
        int EncounterId,
        DateTime EndDate,
        EncounterStatus Status);
}
