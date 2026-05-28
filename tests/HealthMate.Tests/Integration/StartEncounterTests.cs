using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Identity;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Integration;

public sealed class StartEncounterTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    private static int seedCounter;

    [Fact]
    public async Task Start_encounter_returns_created_and_persists_active_encounter()
    {
        var (patientId, providerId) = await SeedPatientAndProviderAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(providerId));

        var response = await client.PostAsync(
            "/api/Encounter/start",
            JsonContent(new
            {
                patientId,
                healthCareProviderId = providerId,
                reasonToVisit = "Synthetic visit reason"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<StartEncounterResponse>(response);
        result.EncounterId.Should().BeGreaterThan(0);
        result.EncounterFhirId.Should().NotBeNullOrWhiteSpace();

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var row = await context.Encounters.FindAsync(result.EncounterId);
        row.Should().NotBeNull();
        row!.Status.Should().Be(EncounterStatus.Active);
        row.ReasonToVisit.Value.Should().Be("Synthetic visit reason");
        row.PatientId.Should().Be(patientId);
        row.HealthCareProviderId.Should().Be(providerId);
    }

    [Fact]
    public async Task Start_encounter_returns_not_found_for_missing_patient()
    {
        var (_, providerId) = await SeedPatientAndProviderAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(providerId));

        var response = await client.PostAsync(
            "/api/Encounter/start",
            JsonContent(new
            {
                patientId = 999999,
                healthCareProviderId = providerId,
                reasonToVisit = "Synthetic visit reason"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("patient_not_found_for_encounter");
    }

    private async Task<(int PatientId, int ProviderId)> SeedPatientAndProviderAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var seed = Interlocked.Increment(ref seedCounter);
        var suffix = Guid.NewGuid().ToString("N");
        var patientUser = new ApplicationUser
        {
            Id = "patient-start-encounter-" + suffix,
            UserName = "patient_start_encounter_" + suffix,
            NormalizedUserName = "PATIENT_START_ENCOUNTER_" + suffix.ToUpperInvariant(),
            Email = "patient_start_encounter_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_START_ENCOUNTER_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-start-encounter-" + suffix,
            UserName = "provider_start_encounter_" + suffix,
            NormalizedUserName = "PROVIDER_START_ENCOUNTER_" + suffix.ToUpperInvariant(),
            Email = "provider_start_encounter_" + suffix + "@example.invalid",
            NormalizedEmail = "PROVIDER_START_ENCOUNTER_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
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

        return (patient.Id, provider.HealthCareProvider_Id);
    }

    private static string CreateProviderToken(int providerId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "HealthCareProvider"),
            new Claim("UserName", "Provider_Zero"),
            new Claim("HealthCareProviderId", providerId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TEST_ONLY_JWT_SIGNING_KEY_NOT_SECRET_00000000000000000000000000000000"));
        var token = new JwtSecurityToken(
            issuer: "HealthMate_Servers",
            audience: "HealthMate_Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static StringContent JsonContent<T>(T value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private sealed record StartEncounterResponse(int EncounterId, string EncounterFhirId);
}
