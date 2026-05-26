using HealthMate.Domain.Aggregates.Patient.Enums;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Common;
using HealthMate.Domain.Common.Enums;
using HealthMate.Domain.Identity;

namespace HealthMate.Domain.Aggregates.Patient;

public sealed class Patient : AggregateRoot<int>
{
    private readonly List<PatientAllergy> allergies = [];

    private Patient()
    {
    }

    public override int Id
    {
        get => Patient_Id;
        protected set => Patient_Id = value;
    }

    public int Patient_Id { get; private set; }
    public string Patient_Fhir_Id { get; private set; } = null!;
    public NationalId NationalId { get; private set; } = null!;
    public string NationalIdImageUrl { get; private set; } = string.Empty;
    public DateOnly BirthDate { get; private set; }
    public Gender Gender { get; private set; }
    public Governorate Governorate { get; private set; } = null!;
    public City City { get; private set; } = null!;
    public bool IsVerified { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }
    public uint RowVersion { get; private set; } = 1;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public string? ApplicationUserId { get; private set; }
    public float? Weight { get; private set; }
    public float? Height { get; private set; }
    public IReadOnlyCollection<PatientAllergy> Allergies => allergies.AsReadOnly();

    public static Patient Create(
        NationalId nationalId,
        DateOnly birthDate,
        Gender gender,
        Governorate governorate,
        City city,
        UserId? userId,
        string? nationalIdImageUrl = null,
        float? weight = null,
        float? height = null)
    {
        EnsureBirthDateIsValid(birthDate);
        EnsureMeasurementsAreValid(weight, height);

        return new Patient
        {
            NationalId = nationalId,
            BirthDate = birthDate,
            Gender = gender,
            Governorate = governorate,
            City = city,
            ApplicationUserId = userId?.Value,
            NationalIdImageUrl = string.IsNullOrWhiteSpace(nationalIdImageUrl) ? string.Empty : nationalIdImageUrl.Trim(),
            Weight = weight,
            Height = height,
            IsVerified = false,
            IsDeleted = false,
            RowVersion = 1
        };
    }

    public void AssignFhirId(string fhirId)
    {
        if (string.IsNullOrWhiteSpace(fhirId))
        {
            throw new DomainException("FHIR patient id is required.");
        }

        Patient_Fhir_Id = fhirId.Trim();
    }

    public void ApplyPersistenceVersion(DateTimeOffset lastUpdated, uint rowVersion)
    {
        LastUpdated = lastUpdated;
        RowVersion = rowVersion;
    }

    public void UpdateDemographics(
        NationalId nationalId,
        DateOnly birthDate,
        Gender gender,
        Governorate governorate,
        City city)
    {
        EnsureBirthDateIsValid(birthDate);

        NationalId = nationalId;
        BirthDate = birthDate;
        Gender = gender;
        UpdateAddress(governorate, city);
    }

    public void UpdateAddress(Governorate governorate, City city)
    {
        Governorate = governorate;
        City = city;
    }

    public void Verify(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        IsVerified = true;
    }

    public void MarkSoftDeleted(IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = clock.UtcNow;
    }

    public void RestoreFromSoftDelete()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    public void AssignUserAccount(UserId userId)
    {
        if (ApplicationUserId is not null && !ApplicationUserId.Equals(userId.Value, StringComparison.Ordinal))
        {
            throw new DomainException("Patient is already assigned to a different user account.");
        }

        ApplicationUserId = userId.Value;
    }

    public void UpdateRegistrationDetails(string? nationalIdImageUrl, float? weight, float? height)
    {
        EnsureMeasurementsAreValid(weight, height);

        NationalIdImageUrl = string.IsNullOrWhiteSpace(nationalIdImageUrl) ? string.Empty : nationalIdImageUrl.Trim();
        Weight = weight;
        Height = height;
    }

    public PatientAllergy AddAllergy(
        string substance,
        AllergySeverity severity,
        string? reaction,
        string? notes,
        IDateTimeProvider clock)
    {
        var allergy = PatientAllergy.Create(this, substance, severity, reaction, notes, clock);
        allergies.Add(allergy);
        return allergy;
    }

    public void RemoveAllergy(int allergyId)
    {
        var allergy = allergies.FirstOrDefault(a => a.Id == allergyId);
        allergy?.Deactivate();
    }

    private static void EnsureBirthDateIsValid(DateOnly birthDate)
    {
        if (birthDate == default)
        {
            throw new DomainException("Birth date is required.");
        }

        if (birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new DomainException("Birth date cannot be in the future.");
        }
    }

    private static void EnsureMeasurementsAreValid(float? weight, float? height)
    {
        if (weight is <= 0)
        {
            throw new DomainException("Weight must be greater than zero when provided.");
        }

        if (height is <= 0)
        {
            throw new DomainException("Height must be greater than zero when provided.");
        }
    }
}
