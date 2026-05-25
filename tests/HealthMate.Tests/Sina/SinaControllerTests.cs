using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;
using HealthMate.Sina.Sessions;
using HealthMate.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HealthMate.Tests.Sina;

public sealed class SinaControllerTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task Session_flow_returns_session_and_reply_for_provider()
    {
        var (patientId, providerId) = await SeedPatientAndProviderAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("HealthCareProvider", providerId));

        var openResponse = await client.PostAsync(
            "/api/Sina/sessions",
            JsonContent(new { patientId }));

        openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await ReadJsonAsync<OpenSessionResponse>(openResponse);
        session.SessionId.Should().NotBeEmpty();

        var messageResponse = await client.PostAsync(
            $"/api/Sina/sessions/{session.SessionId}/messages",
            JsonContent(new { content = "Summarize this chart." }));

        messageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reply = await ReadJsonAsync<SinaTurnResponse>(messageResponse);
        reply.Reply.Should().Contain("[#P-1]");
    }

    [Fact]
    public async Task Session_open_returns_403_for_patient_token()
    {
        var (patientId, _) = await SeedPatientAndProviderAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Patient", patientId));

        var response = await client.PostAsync(
            "/api/Sina/sessions",
            JsonContent(new { patientId }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(int PatientId, int ProviderId)> SeedPatientAndProviderAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var patientUser = new ApplicationUser
        {
            Id = "patient-" + suffix,
            UserName = "patient_zero_" + suffix,
            NormalizedUserName = "PATIENT_ZERO_" + suffix.ToUpperInvariant(),
            Email = "patient_zero_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_ZERO_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-" + suffix,
            UserName = "provider_zero_" + suffix,
            NormalizedUserName = "PROVIDER_ZERO_" + suffix.ToUpperInvariant(),
            Email = "provider_zero_" + suffix + "@example.invalid",
            NormalizedEmail = "PROVIDER_ZERO_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Provider_Zero",
            Last_Name = "Example",
            UserType = UserType.HealthCareProvider,
            IsActive = true,
            EmailConfirmed = true
        };

        context.Users.AddRange(patientUser, providerUser);
        var patient = new Patient
        {
            ApplicationUserId = patientUser.Id,
            NationalId = "00000000000000",
            NationalIdImageUrl = "patient_zero_national_id.png",
            BirthDate = new DateOnly(2000, 1, 1),
            Gender = Gender.Female,
            Governorate = "Fake_Governorate",
            City = "Fake_City",
            IsVerified = true,
            Weight = 70,
            Height = 170
        };
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
        return (patient.Patient_Id, provider.HealthCareProvider_Id);
    }

    private static string CreateToken(string role, int subjectId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("UserName", role + "_Zero")
        };
        if (role == "HealthCareProvider")
        {
            claims.Add(new Claim("HealthCareProviderId", subjectId.ToString()));
        }
        else
        {
            claims.Add(new Claim("PatientId", subjectId.ToString()));
        }

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
}
