using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Identity;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;
using HealthMate.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Api;

public sealed class ApiBaselineTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var response = await fixture.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_then_login_returns_jwt()
    {
        const string email = "patient_zero@example.invalid";
        const string password = "TestPass123!";

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Patient_Zero"), "First_Name" },
            { new StringContent("Example"), "Last_Name" },
            { new StringContent(email), "Email" },
            { new StringContent(password), "Password" },
            { new StringContent(password), "PasswordConfirmed" },
            { new StringContent(((int)UserType.Patient).ToString()), "UserType" },
            { new StringContent("01000000000"), "PhoneNumber" }
        };

        var image = new ByteArrayContent([0xff, 0xd8, 0xff, 0xd9]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(image, "Image", "patient_zero.jpg");

        var registerResponse = await fixture.Client.PostAsync("/api/Account/Register", form);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        using var registerDocument = JsonDocument.Parse(registerJson);
        var userId = registerDocument.RootElement.GetProperty("userId").GetString();
        userId.Should().NotBeNullOrWhiteSpace();

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
            var patient = Patient.Create(
                NationalId.Create("00000000000000"),
                new DateOnly(2000, 1, 1),
                DomainGender.Male,
                Governorate.Create("Fake_Governorate"),
                City.Create("Fake_City"),
                UserId.Create(userId!),
                "patient_zero_national_id.png");
            patient.Verify(FixedDateTimeProvider.Instance);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        var loginPayload = JsonSerializer.Serialize(new
        {
            email,
            password,
            stayLoggedIn = false
        });

        var loginResponse = await fixture.Client.PostAsync(
            "/api/Account/Login",
            new StringContent(loginPayload, Encoding.UTF8, "application/json"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        using var loginDocument = JsonDocument.Parse(loginJson);
        loginDocument.RootElement.GetProperty("jwtToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_patients_unauthorized_returns_401()
    {
        var response = await fixture.Client.GetAsync("/api/Patient");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
