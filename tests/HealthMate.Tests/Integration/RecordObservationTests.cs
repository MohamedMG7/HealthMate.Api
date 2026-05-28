using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Identity;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Sina.Ports;
using HealthMate.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Integration;

public sealed class RecordObservationTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    private static int seedCounter;

    [Fact]
    public async Task Record_observation_returns_created_and_derives_patient_from_encounter()
    {
        var seeded = await SeedPatientProviderAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));
        var observedAt = DateTime.UtcNow.AddMinutes(-5);

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/observations",
            JsonContent(new
            {
                category = ObservationCategory.VitalSigns,
                code = "hr",
                codeDisplayName = "heartrate",
                valueQuantity = 72m,
                valueUnit = "bpm",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = observedAt,
                nameIdentifier = (string?)null
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<RecordObservationResponse>(response);
        result.ObservationId.Should().BeGreaterThan(0);
        result.ObservationFhirId.Should().NotBeNullOrWhiteSpace();
        result.PatientId.Should().Be(seeded.PatientId);

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var row = await context.Observations.SingleAsync(observation => observation.Id == result.ObservationId);
        row.EncounterId.Should().Be(seeded.EncounterId);
        row.PatientId.Should().Be(seeded.PatientId);
        row.ValueQuantity.Should().Be(72m);
        row.Interpretation.Should().Be("normal");
    }

    [Fact]
    public async Task Record_observation_returns_not_found_for_missing_encounter()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(123));

        var response = await client.PostAsync(
            "/api/Encounter/999999/observations",
            JsonContent(new
            {
                category = ObservationCategory.Clinical,
                code = "code",
                codeDisplayName = "Synthetic observation",
                valueQuantity = 1m,
                valueUnit = "unit",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = DateTime.UtcNow.AddMinutes(-5),
                nameIdentifier = (string?)null
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("encounter_not_found");
    }

    [Fact]
    public async Task Record_observation_is_returned_by_sina_reader_with_legacy_typo_column_mapping()
    {
        var seeded = await SeedPatientProviderAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));
        var observedAt = DateTime.UtcNow.AddMinutes(-5);

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/observations",
            JsonContent(new
            {
                category = ObservationCategory.Laboratory,
                code = "glucose",
                codeDisplayName = "glucose",
                valueQuantity = 101.5m,
                valueUnit = "mg/dL",
                interpretation = "high",
                bodySiteId = (int?)null,
                dateOfObservation = observedAt,
                nameIdentifier = (string?)null
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<RecordObservationResponse>(response);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var observations = await reader.SearchObservationsAsync(
            seeded.PatientId,
            "glucose",
            observedAt.AddDays(-1),
            observedAt.AddDays(1),
            10,
            CancellationToken.None);

        var observation = observations.Single(summary => summary.Id == result.ObservationId);
        observation.Value.Should().Be(101.5m);
        observation.Interpretation.Should().Be("high");
    }

    private async Task<(int PatientId, int ProviderId, int EncounterId)> SeedPatientProviderAndEncounterAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var seed = Interlocked.Increment(ref seedCounter);
        var suffix = Guid.NewGuid().ToString("N");
        var patientUser = new ApplicationUser
        {
            Id = "patient-record-observation-" + suffix,
            UserName = "patient_record_observation_" + suffix,
            NormalizedUserName = "PATIENT_RECORD_OBSERVATION_" + suffix.ToUpperInvariant(),
            Email = "patient_record_observation_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_RECORD_OBSERVATION_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-record-observation-" + suffix,
            UserName = "provider_record_observation_" + suffix,
            NormalizedUserName = "PROVIDER_RECORD_OBSERVATION_" + suffix.ToUpperInvariant(),
            Email = "provider_record_observation_" + suffix + "@example.invalid",
            NormalizedEmail = "PROVIDER_RECORD_OBSERVATION_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
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

        var encounter = Encounter.Start(
            patient.Id,
            provider.HealthCareProvider_Id,
            ReasonToVisit.Create("Synthetic visit reason"),
            FixedDateTimeProvider.Instance);
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        return (patient.Id, provider.HealthCareProvider_Id, encounter.Id);
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

    private sealed record RecordObservationResponse(int ObservationId, string ObservationFhirId, int PatientId);
}
