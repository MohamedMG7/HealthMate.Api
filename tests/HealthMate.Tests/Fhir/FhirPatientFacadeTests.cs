using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using HealthMate.Fhir.Extensions;
using HealthMate.Fhir.Mapping;
using HealthMate.Fhir.Ports;
using HealthMate.Fhir.Ports.Dtos;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;
using HealthMate.Tests.Infrastructure;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using DataPatient = HealthMate.Infrastructure.Data.Models.Patient;
using FhirPatient = Hl7.Fhir.Model.Patient;
using SecurityClaim = System.Security.Claims.Claim;
using ThreadingTask = System.Threading.Tasks.Task;

namespace HealthMate.Tests.Fhir;

public sealed class FhirPatientFacadeTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    private static readonly FhirJsonParser Parser = new(new ParserSettings { AcceptUnknownMembers = false });

    [Fact]
    public async ThreadingTask Metadata_returns_parseable_capability_statement()
    {
        var response = await fixture.Client.GetAsync("/fhir/metadata");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/fhir+json");

        var resource = Parser.Parse<Resource>(await response.Content.ReadAsStringAsync());
        resource.Should().BeOfType<CapabilityStatement>();
        ((CapabilityStatement)resource).Rest.Single().Resource.Single().Type.Should().Be("Patient");
    }

    [Fact]
    public async ThreadingTask Read_search_write_history_and_validate_follow_FHIR_contract()
    {
        var (fhirId, providerId) = await SeedPatientAndProviderAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(providerId));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var read = await client.GetAsync($"/fhir/Patient/{fhirId}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        read.Headers.ETag!.Tag.Should().Be("\"1\"");
        var readPatient = Parser.Parse<FhirPatient>(await read.Content.ReadAsStringAsync());
        readPatient.Id.Should().Be(fhirId);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/fhir/Patient/{fhirId}");
        conditional.Headers.IfNoneMatch.Add(read.Headers.ETag);
        var conditionalRead = await client.SendAsync(conditional);
        conditionalRead.StatusCode.Should().Be(HttpStatusCode.NotModified);

        var search = await client.GetAsync("/fhir/Patient?name=Patient_Zero&_count=5");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        Parser.Parse<Bundle>(await search.Content.ReadAsStringAsync()).Entry.Should().Contain(e => e.Resource.Id == fhirId);

        var create = await client.PostAsync("/fhir/Patient", FhirContent(BuildPatientJson("", "Fake_City_Create")));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location!.ToString().Should().StartWith("/fhir/Patient/");
        var created = Parser.Parse<FhirPatient>(await create.Content.ReadAsStringAsync());
        created.Id.Should().NotBeNullOrWhiteSpace();

        var updateJson = BuildPatientJson(created.Id, "Fake_City_Update");
        using var update = new HttpRequestMessage(HttpMethod.Put, $"/fhir/Patient/{created.Id}")
        {
            Content = FhirContent(updateJson)
        };
        update.Headers.IfMatch.Add(create.Headers.ETag!);
        var updateResponse = await client.SendAsync(update);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updateResponse.Headers.ETag!.Tag.Should().Be("\"2\"");

        var staleUpdate = new HttpRequestMessage(HttpMethod.Put, $"/fhir/Patient/{created.Id}")
        {
            Content = FhirContent(updateJson)
        };
        staleUpdate.Headers.IfMatch.Add(create.Headers.ETag!);
        (await client.SendAsync(staleUpdate)).StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

        var delete = await client.DeleteAsync($"/fhir/Patient/{created.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/fhir/Patient/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.Gone);

        var history = await client.GetAsync($"/fhir/Patient/{created.Id}/_history");
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        Parser.Parse<Bundle>(await history.Content.ReadAsStringAsync()).Entry.Should().HaveCount(3);

        var vread = await client.GetAsync($"/fhir/Patient/{created.Id}/_history/1");
        vread.StatusCode.Should().Be(HttpStatusCode.OK);
        Parser.Parse<FhirPatient>(await vread.Content.ReadAsStringAsync()).Address.Single().City.Should().Be("Fake_City_Create");

        var validate = await client.PostAsync("/fhir/Patient/$validate", FhirContent(BuildPatientJson("", "Fake_City_Validate")));
        validate.StatusCode.Should().Be(HttpStatusCode.OK);
        var validateOutcome = Parser.Parse<OperationOutcome>(await validate.Content.ReadAsStringAsync());
        validateOutcome.Issue
            .Where(static i => i.Severity is OperationOutcome.IssueSeverity.Error or OperationOutcome.IssueSeverity.Fatal)
            .Should().BeEmpty();
    }

    [Fact]
    public async ThreadingTask Put_preserves_admin_managed_IsVerified_flag()
    {
        var (fhirId, providerId) = await SeedPatientAndProviderAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(providerId));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var read = await client.GetAsync($"/fhir/Patient/{fhirId}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);

        using var put = new HttpRequestMessage(HttpMethod.Put, $"/fhir/Patient/{fhirId}")
        {
            Content = FhirContent(BuildPatientJson(fhirId, "Fake_City_Verified_PUT"))
        };
        put.Headers.IfMatch.Add(read.Headers.ETag!);
        var putResponse = await client.SendAsync(put);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        var persisted = context.Patients.AsNoTracking().Single(p => p.Patient_Fhir_Id == fhirId);
        persisted.IsVerified.Should().BeTrue("FHIR PUT must not flip the admin-managed verification flag");
        persisted.City.Should().Be("Fake_City_Verified_PUT");
    }

    [Fact]
    public async ThreadingTask Fhir_routes_reject_unsupported_accept_header()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateProviderToken(1));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        var response = await client.GetAsync("/fhir/Patient/whatever");

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        Parser.Parse<OperationOutcome>(await response.Content.ReadAsStringAsync()).Issue.Should().NotBeEmpty();
    }

    private async System.Threading.Tasks.Task<(string FhirId, int ProviderId)> SeedPatientAndProviderAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var patientUser = new ApplicationUser
        {
            Id = "patient-zero-" + suffix,
            UserName = "patient_zero_" + suffix,
            NormalizedUserName = "PATIENT_ZERO_" + suffix.ToUpperInvariant(),
            Email = "patient_zero_" + suffix + "@example.invalid",
            NormalizedEmail = "PATIENT_ZERO_" + suffix.ToUpperInvariant() + "@EXAMPLE.INVALID",
            First_Name = "Patient_Zero",
            Last_Name = "Example",
            PhoneNumber = "+201000000000",
            UserType = UserType.Patient,
            IsActive = true,
            EmailConfirmed = true
        };
        var providerUser = new ApplicationUser
        {
            Id = "provider-zero-" + suffix,
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
        var patient = new DataPatient
        {
            ApplicationUserId = patientUser.Id,
            ApplicationUser = patientUser,
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
        return (patient.Patient_Fhir_Id, provider.HealthCareProvider_Id);
    }

    private static StringContent FhirContent(string json) => new(json, Encoding.UTF8, "application/fhir+json");

    private static string BuildPatientJson(string id, string city)
    {
        var idLine = string.IsNullOrWhiteSpace(id) ? string.Empty : $"\"id\": \"{id}\",";
        return $$"""
        {
          "resourceType": "Patient",
          {{idLine}}
          "active": true,
          "identifier": [
            {
              "system": "{{HealthMateExtensionUrls.EgyptianNationalIdSystem}}",
              "value": "00000000000000"
            }
          ],
          "gender": "female",
          "birthDate": "2000-01-01",
          "address": [
            {
              "city": "{{city}}",
              "country": "EG",
              "extension": [
                {
                  "url": "{{HealthMateExtensionUrls.Governorate}}",
                  "valueString": "Fake_Governorate"
                }
              ]
            }
          ]
        }
        """;
    }

    private static string CreateProviderToken(int providerId)
    {
        var claims = new List<SecurityClaim>
        {
            new(ClaimTypes.Role, "HealthCareProvider"),
            new("UserName", "Provider_Zero"),
            new("HealthCareProviderId", providerId.ToString())
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
}
