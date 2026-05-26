using FluentAssertions;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Common;
using HealthMate.Domain.Identity;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Tests.Domain;

public sealed class PatientTests
{
    [Fact]
    public void Create_rejects_invalid_national_id()
    {
        Action act = () => Patient.Create(
            NationalId.Create("123"),
            new DateOnly(2000, 1, 1),
            DomainGender.Male,
            Governorate.Create("Fake_Governorate"),
            City.Create("Fake_City"),
            UserId.Create("patient-zero-user"));

        act.Should().Throw<DomainException>()
            .WithMessage("National id must be exactly 14 digits.");
    }

    [Fact]
    public void Create_rejects_future_birth_date()
    {
        Action act = () => Patient.Create(
            NationalId.Create("00000000000000"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DomainGender.Female,
            Governorate.Create("Fake_Governorate"),
            City.Create("Fake_City"),
            UserId.Create("patient-zero-user"));

        act.Should().Throw<DomainException>()
            .WithMessage("Birth date cannot be in the future.");
    }
}
