using System.Globalization;
using HealthMate.Fhir.Extensions;
using HealthMate.Fhir.Ports;
using HealthMate.Fhir.Ports.Dtos;
using Hl7.Fhir.Model;

namespace HealthMate.Fhir.Mapping;

public sealed class PatientResourceMapper
{
    public Patient ToResource(FhirPatientSnapshot snapshot)
    {
        var patient = new Patient
        {
            Id = snapshot.FhirId,
            Meta = new Meta
            {
                VersionId = snapshot.VersionId.ToString(CultureInfo.InvariantCulture),
                LastUpdated = snapshot.LastUpdated
            },
            Active = !snapshot.IsDeleted,
            Identifier =
            [
                new Identifier
                {
                    System = HealthMateExtensionUrls.EgyptianNationalIdSystem,
                    Value = snapshot.NationalId
                }
            ],
            Gender = ToAdministrativeGender(snapshot.Gender),
            BirthDate = snapshot.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Address =
            [
                new Address
                {
                    City = snapshot.City,
                    Country = "EG",
                    Extension =
                    [
                        new Hl7.Fhir.Model.Extension(
                            HealthMateExtensionUrls.Governorate,
                            new FhirString(snapshot.Governorate))
                    ]
                }
            ]
        };

        if (!string.IsNullOrWhiteSpace(snapshot.Name))
        {
            patient.Name = [new HumanName { Text = snapshot.Name }];
        }

        if (!string.IsNullOrWhiteSpace(snapshot.PhoneE164))
        {
            patient.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = snapshot.PhoneE164
            });
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Email))
        {
            patient.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value = snapshot.Email
            });
        }

        return patient;
    }

    public FhirPatientSnapshot ToSnapshot(Patient resource)
    {
        var issues = new List<FhirValidationIssue>();

        var nationalId = ReadNationalId(resource, issues);
        var birthDate = ReadBirthDate(resource, issues);
        var gender = ReadGender(resource, issues);
        var address = resource.Address.FirstOrDefault();
        var city = address?.City;
        var governorate = ReadGovernorate(address);

        if (string.IsNullOrWhiteSpace(city))
        {
            issues.Add(new FhirValidationIssue("Patient.address[0].city is required.", "Patient.address[0].city"));
        }

        if (string.IsNullOrWhiteSpace(governorate))
        {
            issues.Add(new FhirValidationIssue("Patient.address[0] governorate extension is required.", "Patient.address[0].extension"));
        }

        if (issues.Count > 0)
        {
            throw new FhirValidationException(issues);
        }

        var versionId = uint.TryParse(resource.Meta?.VersionId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedVersion)
            ? parsedVersion
            : 0;

        return new FhirPatientSnapshot(
            resource.Id ?? string.Empty,
            nationalId!,
            ReadName(resource),
            birthDate!.Value,
            gender!,
            governorate!,
            city!,
            ReadTelecom(resource, ContactPoint.ContactPointSystem.Phone),
            ReadTelecom(resource, ContactPoint.ContactPointSystem.Email),
            // IsVerified is admin-managed and never carried on the write path; the adapter ignores this value.
            IsVerified: false,
            resource.Meta?.LastUpdated ?? DateTimeOffset.UtcNow,
            versionId,
            resource.Active.HasValue && !resource.Active.Value);
    }

    private static AdministrativeGender ToAdministrativeGender(string gender)
    {
        return gender.ToLowerInvariant() switch
        {
            "male" => AdministrativeGender.Male,
            "female" => AdministrativeGender.Female,
            _ => AdministrativeGender.Unknown
        };
    }

    private static string ReadGender(Patient resource, List<FhirValidationIssue> issues)
    {
        if (resource.Gender is null)
        {
            issues.Add(new FhirValidationIssue("Patient.gender is required.", "Patient.gender"));
            return string.Empty;
        }

        return resource.Gender.Value switch
        {
            AdministrativeGender.Male => "male",
            AdministrativeGender.Female => "female",
            _ => AddIssue("Patient.gender must be male or female.", "Patient.gender", issues)
        };
    }

    private static string ReadNationalId(Patient resource, List<FhirValidationIssue> issues)
    {
        var identifier = resource.Identifier.FirstOrDefault(i =>
            string.IsNullOrWhiteSpace(i.System) || i.System == HealthMateExtensionUrls.EgyptianNationalIdSystem);

        if (identifier is null || string.IsNullOrWhiteSpace(identifier.Value))
        {
            issues.Add(new FhirValidationIssue("Patient.identifier with Egyptian NationalId is required.", "Patient.identifier"));
            return string.Empty;
        }

        return identifier.Value;
    }

    private static DateOnly? ReadBirthDate(Patient resource, List<FhirValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(resource.BirthDate))
        {
            issues.Add(new FhirValidationIssue("Patient.birthDate is required.", "Patient.birthDate"));
            return null;
        }

        if (!DateOnly.TryParseExact(resource.BirthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
        {
            issues.Add(new FhirValidationIssue("Patient.birthDate must be a full yyyy-MM-dd date.", "Patient.birthDate"));
            return null;
        }

        return birthDate;
    }

    private static string? ReadGovernorate(Address? address)
    {
        return address?.Extension
            .FirstOrDefault(e => e.Url == HealthMateExtensionUrls.Governorate)
            ?.Value switch
        {
            FhirString governorate => governorate.Value,
            _ => null
        };
    }

    private static string? ReadName(Patient resource)
    {
        var name = resource.Name.FirstOrDefault();
        if (name is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name.Text))
        {
            return name.Text;
        }

        var given = string.Join(' ', name.Given.Where(static g => !string.IsNullOrWhiteSpace(g)));
        return string.Join(' ', new[] { given, name.Family }.Where(static p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string? ReadTelecom(Patient resource, ContactPoint.ContactPointSystem system)
    {
        return resource.Telecom.FirstOrDefault(t => t.System == system)?.Value;
    }

    private static string AddIssue(string detail, string fhirPath, List<FhirValidationIssue> issues)
    {
        issues.Add(new FhirValidationIssue(detail, fhirPath));
        return string.Empty;
    }
}
