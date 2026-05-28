using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
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

public sealed class RecordConditionTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    private static int seedCounter;

    [Fact]
    public async Task Record_condition_returns_created_and_derives_patient_from_encounter()
    {
        var seeded = await SeedPatientProviderDiseaseAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));
        var recordedAt = DateTime.UtcNow.AddMinutes(-5);

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/conditions",
            JsonContent(new
            {
                diseaseId = seeded.DiseaseId,
                severity = Severity.Moderate,
                clinicalStatus = ClinicalStatus.Active,
                dateRecorded = recordedAt,
                note = "  test-note  "
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<RecordConditionResponse>(response);
        result.ConditionId.Should().BeGreaterThan(0);
        result.ConditionFhirId.Should().NotBeNullOrWhiteSpace();
        result.PatientId.Should().Be(seeded.PatientId);

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var row = await context.Conditions.SingleAsync(condition => condition.Id == result.ConditionId);
        row.PatientId.Should().Be(seeded.PatientId);
        row.EncounterId.Should().Be(seeded.EncounterId);
        row.DiseaseId.Should().Be(seeded.DiseaseId);
        row.Severity.Should().Be(Severity.Moderate);
        row.ClinicalStatus.Should().Be(ClinicalStatus.Active);
        row.Note.Should().Be("test-note");
    }

    [Fact]
    public async Task Record_condition_returns_not_found_for_missing_disease()
    {
        var seeded = await SeedPatientProviderDiseaseAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/conditions",
            JsonContent(new
            {
                diseaseId = 999999,
                severity = Severity.Mild,
                clinicalStatus = ClinicalStatus.Active,
                dateRecorded = DateTime.UtcNow.AddMinutes(-5),
                note = (string?)null
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("disease_not_found_for_condition");
    }

    [Fact]
    public async Task Record_condition_is_returned_by_sina_reader_with_disease_join()
    {
        var seeded = await SeedPatientProviderDiseaseAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));
        var recordedAt = DateTime.UtcNow.AddMinutes(-5);

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/conditions",
            JsonContent(new
            {
                diseaseId = seeded.DiseaseId,
                severity = Severity.Severe,
                clinicalStatus = ClinicalStatus.Active,
                dateRecorded = recordedAt,
                note = (string?)null
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<RecordConditionResponse>(response);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var chart = await reader.GetPatientChartAsync(seeded.PatientId, CancellationToken.None);

        chart.Should().NotBeNull();
        var condition = chart!.ActiveConditions.Single(summary => summary.Id == result.ConditionId);
        condition.Name.Should().Be(seeded.DiseaseName);
        condition.Severity.Should().Be(Severity.Severe.ToString());
        condition.RecordedAt.Should().BeCloseTo(recordedAt, TimeSpan.FromSeconds(1));
    }

    private async Task<(int PatientId, int ProviderId, int DiseaseId, string DiseaseName, int EncounterId)> SeedPatientProviderDiseaseAndEncounterAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var seed = Interlocked.Increment(ref seedCounter);
        var suffix = Guid.NewGuid().ToString("N");
        var patientUser = new ApplicationUser
        {
            Id = "patient-record-condition-" + suffix,
            UserName = "patient_record_condition_" + suffix,
            NormalizedUserName = "PATIENT_RECORD_CONDITION_" + suffix.ToUpperInvariant(),
            Email = "patient_record_condition_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_RECORD_CONDITION_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-record-condition-" + suffix,
            UserName = "provider_record_condition_" + suffix,
            NormalizedUserName = "PROVIDER_RECORD_CONDITION_" + suffix.ToUpperInvariant(),
            Email = "provider_record_condition_" + suffix + "@example.invalid",
            NormalizedEmail = "PROVIDER_RECORD_CONDITION_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
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

        var disease = new Disease
        {
            Description = "Synthetic disease for tests",
            Scientific_Name = "Synthetic_Disease_Scientific_" + seed,
            Display_Name = "Synthetic_Disease_" + seed,
            Code = "SYN" + seed,
            ICD11_Code = "XSYN" + seed
        };

        context.Patients.Add(patient);
        context.HealthCareProviders.Add(provider);
        context.Diseases.Add(disease);
        await context.SaveChangesAsync();

        var encounter = Encounter.Start(
            patient.Id,
            provider.HealthCareProvider_Id,
            ReasonToVisit.Create("Synthetic visit reason"),
            FixedDateTimeProvider.Instance);
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        return (patient.Id, provider.HealthCareProvider_Id, disease.Disease_Id, disease.Display_Name, encounter.Id);
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

    private sealed record RecordConditionResponse(int ConditionId, string ConditionFhirId, int PatientId);
}
