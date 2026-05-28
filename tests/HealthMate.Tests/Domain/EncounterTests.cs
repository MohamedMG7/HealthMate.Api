using FluentAssertions;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
using HealthMate.Domain.Common;
using HealthMate.Tests.Infrastructure;

namespace HealthMate.Tests.Domain;

public sealed class EncounterTests
{
    [Fact]
    public void Start_creates_encounter_with_active_status_and_provided_clock_time()
    {
        var reason = ReasonToVisit.Create("Synthetic visit reason");

        var encounter = Encounter.Start(1, 2, reason, FixedDateTimeProvider.Instance);

        encounter.PatientId.Should().Be(1);
        encounter.HealthCareProviderId.Should().Be(2);
        encounter.StartDate.Should().Be(FixedDateTimeProvider.Instance.UtcNow.UtcDateTime);
        encounter.EndDate.Should().Be(encounter.StartDate);
        encounter.Status.Should().Be(EncounterStatus.Active);
        encounter.TreatmentPlan.Should().BeEmpty();
        encounter.ReasonToVisit.Should().Be(reason);
        encounter.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Start_rejects_invalid_patient_id(int patientId)
    {
        var reason = ReasonToVisit.Create("Synthetic visit reason");

        Action act = () => Encounter.Start(patientId, 2, reason, FixedDateTimeProvider.Instance);

        act.Should().Throw<DomainException>()
            .WithMessage("Patient id must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Start_rejects_invalid_hcp_id(int healthCareProviderId)
    {
        var reason = ReasonToVisit.Create("Synthetic visit reason");

        Action act = () => Encounter.Start(1, healthCareProviderId, reason, FixedDateTimeProvider.Instance);

        act.Should().Throw<DomainException>()
            .WithMessage("Health care provider id must be greater than zero.");
    }

    [Fact]
    public void Start_rejects_reason_to_visit_below_min_length()
    {
        Action act = () => ReasonToVisit.Create("ab");

        act.Should().Throw<DomainException>()
            .WithMessage("Reason to visit must be at least 3 characters.");
    }

    [Fact]
    public void Start_rejects_reason_to_visit_above_max_length()
    {
        var value = new string('a', ReasonToVisit.MaxLength + 1);

        Action act = () => ReasonToVisit.Create(value);

        act.Should().Throw<DomainException>()
            .WithMessage("Reason to visit must be at most 500 characters.");
    }

    [Fact]
    public void ReasonToVisit_Create_rejects_whitespace_only()
    {
        Action act = () => ReasonToVisit.Create("   ");

        act.Should().Throw<DomainException>()
            .WithMessage("Reason to visit is required.");
    }
}
