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

    [Fact]
    public void End_finishes_active_encounter_with_treatment_plan_and_status()
    {
        var encounter = CreateActiveEncounter();
        var clock = new TestClock(new DateTimeOffset(2026, 2, 1, 10, 30, 0, TimeSpan.Zero));

        encounter.End("Synthetic treatment plan", "Synthetic note", clock);

        encounter.Status.Should().Be(EncounterStatus.Finished);
        encounter.TreatmentPlan.Should().Be("Synthetic treatment plan");
        encounter.Note.Should().Be("Synthetic note");
        encounter.EndDate.Should().Be(clock.UtcNow.UtcDateTime);
    }

    [Fact]
    public void End_trims_treatment_plan_and_note_whitespace()
    {
        var encounter = CreateActiveEncounter();

        encounter.End("  Synthetic treatment plan  ", "  Synthetic note  ", FixedDateTimeProvider.Instance);

        encounter.TreatmentPlan.Should().Be("Synthetic treatment plan");
        encounter.Note.Should().Be("Synthetic note");
    }

    [Fact]
    public void End_treats_blank_note_as_null()
    {
        var encounter = CreateActiveEncounter();

        encounter.End("Synthetic treatment plan", "   ", FixedDateTimeProvider.Instance);

        encounter.Note.Should().BeNull();
    }

    [Fact]
    public void End_throws_when_treatment_plan_is_whitespace()
    {
        var encounter = CreateActiveEncounter();

        Action act = () => encounter.End("   ", null, FixedDateTimeProvider.Instance);

        act.Should().Throw<DomainException>()
            .WithMessage("Treatment plan is required when ending an encounter.");
    }

    [Fact]
    public void End_throws_when_already_finished()
    {
        var encounter = CreateActiveEncounter();
        encounter.End("Synthetic treatment plan", null, FixedDateTimeProvider.Instance);

        Action act = () => encounter.End("Another synthetic treatment plan", null, FixedDateTimeProvider.Instance);

        act.Should().Throw<EncounterAlreadyEndedException>()
            .Which.CurrentStatus.Should().Be(EncounterStatus.Finished);
    }

    [Fact]
    public void End_throws_when_cancelled()
    {
        var encounter = CreateActiveEncounter();
        SetStatus(encounter, EncounterStatus.Cancelled);

        Action act = () => encounter.End("Synthetic treatment plan", null, FixedDateTimeProvider.Instance);

        act.Should().Throw<EncounterAlreadyEndedException>()
            .Which.CurrentStatus.Should().Be(EncounterStatus.Cancelled);
    }

    [Fact]
    public void End_updates_end_date_from_provided_clock()
    {
        var encounter = CreateActiveEncounter();
        var clock = new TestClock(new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero));

        encounter.End("Synthetic treatment plan", null, clock);

        encounter.EndDate.Should().Be(clock.UtcNow.UtcDateTime);
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

    private static Encounter CreateActiveEncounter()
    {
        return Encounter.Start(
            1,
            2,
            ReasonToVisit.Create("Synthetic visit reason"),
            FixedDateTimeProvider.Instance);
    }

    private static void SetStatus(Encounter encounter, EncounterStatus status)
    {
        typeof(Encounter).GetProperty(nameof(Encounter.Status))!.SetValue(encounter, status);
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
