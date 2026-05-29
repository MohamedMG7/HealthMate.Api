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

public sealed class WritePrescriptionTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    private static int seedCounter;

    [Fact]
    public async Task Write_prescription_returns_created_and_persists_medicine_lines()
    {
        var seeded = await SeedPatientProviderMedicinesAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/prescription",
            JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId = seeded.FirstMedicineId, dosage = "10mg", frequencyInHours = 8, durationInDays = 5 },
                    new { medicineId = seeded.SecondMedicineId, dosage = "20mg", frequencyInHours = 12, durationInDays = 7 }
                }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<WritePrescriptionResponse>(response);
        result.PrescriptionId.Should().BeGreaterThan(0);
        result.PatientId.Should().Be(seeded.PatientId);
        result.MedicineCount.Should().Be(2);

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var prescription = await context.Prescriptions
            .Include(p => p.Medicines)
            .SingleAsync(p => p.Id == result.PrescriptionId);
        prescription.PatientId.Should().Be(seeded.PatientId);
        prescription.EncounterId.Should().Be(seeded.EncounterId);
        prescription.Publisher.Should().Be("Provider_Zero");
        prescription.Medicines.Should().HaveCount(2);

        var medicines = await context.PrescriptionMedicines
            .Where(m => m.PrescriptionId == result.PrescriptionId)
            .OrderBy(m => m.MedicineId)
            .ToListAsync();
        medicines.Should().HaveCount(2);
        medicines.Should().OnlyContain(m => m.IsPrescribed);
        medicines.Should().OnlyContain(m => m.PatientId == seeded.PatientId);
        medicines.Select(m => m.Dosage).Should().BeEquivalentTo(["10mg", "20mg"]);
    }

    [Fact]
    public async Task Write_prescription_returns_not_found_for_missing_encounter()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(123));

        var response = await client.PostAsync(
            "/api/Encounter/999999/prescription",
            JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId = 999999, dosage = "10mg", frequencyInHours = 8, durationInDays = 5 }
                }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("encounter_not_found");
    }

    [Fact]
    public async Task Write_prescription_returns_conflict_for_duplicate_encounter_prescription()
    {
        var seeded = await SeedPatientProviderMedicinesAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));

        var first = await client.PostAsync($"/api/Encounter/{seeded.EncounterId}/prescription", ValidBody(seeded.FirstMedicineId));
        var second = await client.PostAsync($"/api/Encounter/{seeded.EncounterId}/prescription", ValidBody(seeded.FirstMedicineId));

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var document = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("prescription_already_exists_for_encounter");
    }

    [Fact]
    public async Task Write_prescription_returns_not_found_for_unknown_medicine()
    {
        var seeded = await SeedPatientProviderMedicinesAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));

        var response = await client.PostAsync($"/api/Encounter/{seeded.EncounterId}/prescription", ValidBody(999999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("medicine_not_found_for_prescription");
    }

    [Fact]
    public async Task Write_prescription_returns_bad_request_for_empty_medicines()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(123));

        var response = await client.PostAsync(
            "/api/Encounter/1/prescription",
            JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = Array.Empty<object>()
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Write_prescription_is_returned_by_sina_prescription_and_active_medication_readers()
    {
        var seeded = await SeedPatientProviderMedicinesAndEncounterAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(seeded.ProviderId));

        var response = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/prescription",
            JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId = seeded.FirstMedicineId, dosage = "10mg", frequencyInHours = 8, durationInDays = 5 },
                    new { medicineId = seeded.SecondMedicineId, dosage = "20mg", frequencyInHours = 12, durationInDays = 7 }
                }
            }));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadJsonAsync<WritePrescriptionResponse>(response);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var history = await reader.GetPrescriptionHistoryAsync(seeded.PatientId, seeded.FirstMedicineName, CancellationToken.None);
        var prescription = history.Single(summary => summary.Id == result.PrescriptionId);
        prescription.Medicines.Select(medicine => medicine.MedicineName).Should().Contain([seeded.FirstMedicineName, seeded.SecondMedicineName]);

        var activeMedications = await reader.GetActiveMedicationsAsync(seeded.PatientId, CancellationToken.None);
        activeMedications.Should().Contain(medicine =>
            medicine.MedicineId == seeded.FirstMedicineId &&
            medicine.MedicineName == seeded.FirstMedicineName &&
            medicine.Dosage == "10mg");
    }

    private async Task<(int PatientId, int ProviderId, int EncounterId, int FirstMedicineId, int SecondMedicineId, string FirstMedicineName, string SecondMedicineName)> SeedPatientProviderMedicinesAndEncounterAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var seed = Interlocked.Increment(ref seedCounter);
        var suffix = Guid.NewGuid().ToString("N");
        var patientUser = new ApplicationUser
        {
            Id = "patient-write-prescription-" + suffix,
            UserName = "patient_write_prescription_" + suffix,
            NormalizedUserName = "PATIENT_WRITE_PRESCRIPTION_" + suffix.ToUpperInvariant(),
            Email = "patient_write_prescription_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_WRITE_PRESCRIPTION_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-write-prescription-" + suffix,
            UserName = "provider_write_prescription_" + suffix,
            NormalizedUserName = "PROVIDER_WRITE_PRESCRIPTION_" + suffix.ToUpperInvariant(),
            Email = "provider_write_prescription_" + suffix + "@example.invalid",
            NormalizedEmail = "PROVIDER_WRITE_PRESCRIPTION_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
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

        var firstMedicine = new Medicine
        {
            Name = "Synthetic_Medicine_A_" + seed,
            Description = "Synthetic medicine for tests",
            ActiveIngrediantes = "Synthetic_Ingredient_A",
            UsedToCure = "Synthetic use"
        };
        var secondMedicine = new Medicine
        {
            Name = "Synthetic_Medicine_B_" + seed,
            Description = "Synthetic medicine for tests",
            ActiveIngrediantes = "Synthetic_Ingredient_B",
            UsedToCure = "Synthetic use"
        };

        context.Patients.Add(patient);
        context.HealthCareProviders.Add(provider);
        context.Medicines.AddRange(firstMedicine, secondMedicine);
        await context.SaveChangesAsync();

        var encounter = Encounter.Start(
            patient.Id,
            provider.HealthCareProvider_Id,
            ReasonToVisit.Create("Synthetic visit reason"),
            FixedDateTimeProvider.Instance);
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        return (
            patient.Id,
            provider.HealthCareProvider_Id,
            encounter.Id,
            firstMedicine.Id,
            secondMedicine.Id,
            firstMedicine.Name,
            secondMedicine.Name);
    }

    private static StringContent ValidBody(int medicineId)
    {
        return JsonContent(new
        {
            publisher = "Provider_Zero",
            medicines = new[]
            {
                new { medicineId, dosage = "10mg", frequencyInHours = 8, durationInDays = 5 }
            }
        });
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

    private sealed record WritePrescriptionResponse(int PrescriptionId, int PatientId, int MedicineCount);
}
