using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Common;
using HealthMate.Domain.Identity;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Integration;

internal static class EncounterIntegrationTestHelpers
{
    private static int seedCounter;

    public static async Task<SeededEncounter> SeedActiveEncounterAsync(IServiceProvider services, string prefix)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var seeded = await SeedPatientAndProviderAsync(context, prefix);

        var encounter = Encounter.Start(
            seeded.PatientId,
            seeded.ProviderId,
            ReasonToVisit.Create("Synthetic visit reason"),
            FixedDateTimeProvider.Instance);

        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        return new SeededEncounter(seeded.PatientId, seeded.ProviderId, encounter.Id);
    }

    public static async Task<(int PatientId, int ProviderId)> SeedPatientAndHcpAsync(IServiceProvider services, string prefix)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var seeded = await SeedPatientAndProviderAsync(context, prefix);
        return (seeded.PatientId, seeded.ProviderId);
    }

    public static async Task<int> SeedDiseaseAsync(HealthMateContext context, string displayName, string code = "TEST")
    {
        var seed = Interlocked.Increment(ref seedCounter);
        var disease = new Disease
        {
            Description = "Synthetic disease for tests",
            Scientific_Name = $"Syn_Sci_{code}_{seed}",
            Display_Name = displayName,
            Code = $"{code}{seed}",
            ICD11_Code = $"X{code}{seed}"
        };
        context.Diseases.Add(disease);
        await context.SaveChangesAsync();
        return disease.Disease_Id;
    }

    public static async Task<int> SeedMedicineAsync(HealthMateContext context, string name, string activeIngredients = "Acetaminophen 500mg")
    {
        var medicine = new Medicine
        {
            Name = name,
            Description = "Synthetic medicine for tests",
            ActiveIngrediantes = activeIngredients,
            UsedToCure = "Synthetic use"
        };
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync();
        return medicine.Id;
    }

    public static async Task<SeededLateEntryEncounter> SeedFinishedEncounterWithCatalogAsync(IServiceProvider services, string prefix)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var seeded = await SeedPatientAndProviderAsync(context, prefix);
        var seed = Interlocked.Increment(ref seedCounter);

        var disease = new Disease
        {
            Description = "Synthetic disease for tests",
            Scientific_Name = "Synthetic_Disease_Scientific_Late_" + seed,
            Display_Name = "Synthetic_Disease_Late_" + seed,
            Code = "SL" + seed,
            ICD11_Code = "XSL" + seed
        };
        var medicine = new Medicine
        {
            Name = "Synthetic_Medicine_Late_" + seed,
            Description = "Synthetic medicine for tests",
            ActiveIngrediantes = "Synthetic_Ingredient_Late",
            UsedToCure = "Synthetic use"
        };

        context.Diseases.Add(disease);
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync();

        var encounter = Encounter.Start(
            seeded.PatientId,
            seeded.ProviderId,
            ReasonToVisit.Create("Synthetic visit reason"),
            FixedDateTimeProvider.Instance);
        encounter.End(
            "Synthetic treatment plan",
            null,
            new TestClock(FixedDateTimeProvider.Instance.UtcNow.AddHours(1)));

        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        return new SeededLateEntryEncounter(
            seeded.PatientId,
            seeded.ProviderId,
            encounter.Id,
            disease.Disease_Id,
            medicine.Id);
    }

    public static string CreateProviderToken(int providerId)
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

    public static void AuthorizeAsProvider(HttpClient client, int providerId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(providerId));
    }

    public static StringContent JsonContent<T>(T value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }

    public static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private static async Task<SeededPatientProvider> SeedPatientAndProviderAsync(HealthMateContext context, string prefix)
    {
        var seed = Interlocked.Increment(ref seedCounter);
        var suffix = Guid.NewGuid().ToString("N");
        var normalizedPrefix = prefix.Replace('-', '_');
        var upperPrefix = normalizedPrefix.ToUpperInvariant();
        var patientUser = new ApplicationUser
        {
            Id = "patient-" + prefix + "-" + suffix,
            UserName = "patient_" + normalizedPrefix + "_" + suffix,
            NormalizedUserName = "PATIENT_" + upperPrefix + "_" + suffix.ToUpperInvariant(),
            Email = "patient_" + normalizedPrefix + "_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_" + upperPrefix + "_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-" + prefix + "-" + suffix,
            UserName = "provider_" + normalizedPrefix + "_" + suffix,
            NormalizedUserName = "PROVIDER_" + upperPrefix + "_" + suffix.ToUpperInvariant(),
            Email = "provider_" + normalizedPrefix + "_" + suffix + "@example.invalid",
            NormalizedEmail = "PROVIDER_" + upperPrefix + "_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
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

        return new SeededPatientProvider(patient.Id, provider.HealthCareProvider_Id);
    }

    private sealed record SeededPatientProvider(int PatientId, int ProviderId);

    private sealed class TestClock(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}

internal sealed record SeededEncounter(int PatientId, int ProviderId, int EncounterId);

internal sealed record SeededLateEntryEncounter(
    int PatientId,
    int ProviderId,
    int EncounterId,
    int DiseaseId,
    int MedicineId);
