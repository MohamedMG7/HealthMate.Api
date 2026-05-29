using System.Net;
using FluentAssertions;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Identity;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Integration;

public sealed class ListPatientEncountersTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    private static int seedCounter;

    [Fact]
    public async Task Happy_path_page_1_returns_10_items_ordered_descending_with_has_more()
    {
        var (patientId, providerId) = await SeedAsync(25, "lpe-happy");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var response = await client.GetAsync($"/api/Patient/{patientId}/encounters?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.Items.Should().HaveCount(10);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.HasMore.Should().BeTrue();
        page.Items.Should().BeInDescendingOrder(i => i.StartDate);
    }

    [Fact]
    public async Task Last_page_returns_remaining_items_with_has_more_false()
    {
        var (patientId, providerId) = await SeedAsync(25, "lpe-last-page");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var response = await client.GetAsync($"/api/Patient/{patientId}/encounters?page=3&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.Items.Should().HaveCount(5);
        page.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Empty_patient_returns_empty_items_and_has_more_false()
    {
        var (patientId, providerId) = await SeedAsync(0, "lpe-empty");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var response = await client.GetAsync($"/api/Patient/{patientId}/encounters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.Items.Should().BeEmpty();
        page.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Other_patients_data_not_leaked()
    {
        var (patientA, providerA) = await SeedAsync(5, "lpe-iso-a");
        var (_, _) = await SeedAsync(5, "lpe-iso-b");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerA);

        var response = await client.GetAsync($"/api/Patient/{patientA}/encounters?pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.Items.Should().HaveCount(5);

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var returnedIds = page.Items.Select(i => i.EncounterId).ToArray();
        var dbRows = await context.Encounters.Where(e => returnedIds.Contains(e.Id)).ToArrayAsync();
        dbRows.Should().AllSatisfy(e => e.PatientId.Should().Be(patientA));
    }

    [Fact]
    public async Task Soft_deleted_encounter_is_excluded()
    {
        var (patientId, providerId) = await SeedAsync(1, "lpe-softdel");
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE \"Encounters\" SET \"isDeleted\" = true WHERE \"PatientId\" = {0}", patientId);

        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var response = await client.GetAsync($"/api/Patient/{patientId}/encounters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ReasonToVisit_round_trips_correctly()
    {
        const string reason = "Persistent migraine with aura";
        var (patientId, providerId) = await SeedWithReasonAsync(reason, "lpe-reason");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var response = await client.GetAsync($"/api/Patient/{patientId}/encounters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.Items.Should().ContainSingle()
            .Which.ReasonToVisit.Should().Be(reason);
    }

    [Fact]
    public async Task Page_size_cap_enforced_at_50()
    {
        var (patientId, providerId) = await SeedAsync(0, "lpe-cap");
        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var response = await client.GetAsync($"/api/Patient/{patientId}/encounters?pageSize=200");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await EncounterIntegrationTestHelpers.ReadJsonAsync<PageDto>(response);
        page.PageSize.Should().Be(50);
    }

    // Seeds a patient+provider with exactly `count` encounters, dates spread 1 day apart.
    private async Task<(int PatientId, int ProviderId)> SeedAsync(int count, string prefix)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var seed = Interlocked.Increment(ref seedCounter);
        var suffix = Guid.NewGuid().ToString("N");
        var norm = prefix.Replace('-', '_');
        var upper = norm.ToUpperInvariant();

        var patientUser = new ApplicationUser
        {
            Id = $"patient-{prefix}-{suffix}",
            UserName = $"patient_{norm}_{suffix}",
            NormalizedUserName = $"PATIENT_{upper}_{suffix.ToUpperInvariant()}",
            Email = $"patient_{norm}_{suffix}@example.invalid",
            NormalizedEmail = $"PATIENT_{upper}_{suffix.ToUpperInvariant()}@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = $"provider-{prefix}-{suffix}",
            UserName = $"provider_{norm}_{suffix}",
            NormalizedUserName = $"PROVIDER_{upper}_{suffix.ToUpperInvariant()}",
            Email = $"provider_{norm}_{suffix}@example.invalid",
            NormalizedEmail = $"PROVIDER_{upper}_{suffix.ToUpperInvariant()}@EXAMPLE.INVALID",
            First_Name = "Provider_Zero",
            Last_Name = "Example",
            UserType = UserType.HealthCareProvider,
            IsActive = true,
            EmailConfirmed = true
        };

        context.Users.AddRange(patientUser, providerUser);

        var patient = Patient.Create(
            NationalId.Create(seed.ToString("D14")),
            new DateOnly(2000, 1, 1),
            DomainGender.Female,
            Governorate.Create("Fake_Governorate"),
            City.Create("Fake_City"),
            UserId.Create(patientUser.Id),
            "patient_zero_national_id.png",
            70,
            170);
        patient.Verify(FixedDateTimeProvider.Instance);

        var provider = new HealthCareProvider
        {
            ApplicationUserId = providerUser.Id,
            DateOnJoin = DateTime.UtcNow,
            Specialization = "Test_Specialization",
            Degree = "Test_Degree",
            Governorate = "Fake_Governorate",
            City = "Fake_City",
            IsActive = true
        };

        context.Patients.Add(patient);
        context.HealthCareProviders.Add(provider);
        await context.SaveChangesAsync();

        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < count; i++)
        {
            var encounter = Encounter.CreateLegacy(
                patient.Id,
                provider.HealthCareProvider_Id,
                baseDate.AddDays(i),
                baseDate.AddDays(i).AddHours(1),
                null,
                $"Synthetic reason {i}",
                null,
                null);
            context.Encounters.Add(encounter);
        }

        await context.SaveChangesAsync();
        return (patient.Id, provider.HealthCareProvider_Id);
    }

    private async Task<(int PatientId, int ProviderId)> SeedWithReasonAsync(string reason, string prefix)
    {
        var (patientId, providerId) = await SeedAsync(0, prefix);
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var encounter = Encounter.CreateLegacy(
            patientId, providerId, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null, reason, null, null);
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();
        return (patientId, providerId);
    }

    private sealed record ItemDto(int EncounterId, DateTime StartDate, DateTime EndDate, int Status, string ReasonToVisit, int HealthCareProviderId);
    private sealed record PageDto(IReadOnlyList<ItemDto> Items, int Page, int PageSize, bool HasMore);
}
