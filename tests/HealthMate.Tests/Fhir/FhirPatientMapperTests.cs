using FluentAssertions;
using HealthMate.Fhir.Mapping;
using HealthMate.Fhir.Ports.Dtos;

namespace HealthMate.Tests.Fhir;

public sealed class FhirPatientMapperTests
{
    [Fact]
    public void Patient_mapper_round_trips_representable_fields()
    {
        var mapper = new PatientResourceMapper();
        var snapshot = new FhirPatientSnapshot(
            "patient-zero-fhir-id",
            "00000000000000",
            "Patient_Zero Example",
            new DateOnly(2000, 1, 1),
            "female",
            "Fake_Governorate",
            "Fake_City",
            "+201000000000",
            "patient_zero@example.invalid",
            true,
            DateTimeOffset.UtcNow,
            7,
            false);

        var roundTrip = mapper.ToSnapshot(mapper.ToResource(snapshot));

        roundTrip.FhirId.Should().Be(snapshot.FhirId);
        roundTrip.NationalId.Should().Be(snapshot.NationalId);
        roundTrip.Name.Should().Be(snapshot.Name);
        roundTrip.BirthDate.Should().Be(snapshot.BirthDate);
        roundTrip.Gender.Should().Be(snapshot.Gender);
        roundTrip.Governorate.Should().Be(snapshot.Governorate);
        roundTrip.City.Should().Be(snapshot.City);
        roundTrip.PhoneE164.Should().Be(snapshot.PhoneE164);
        roundTrip.Email.Should().Be(snapshot.Email);
        roundTrip.VersionId.Should().Be(snapshot.VersionId);
    }
}
