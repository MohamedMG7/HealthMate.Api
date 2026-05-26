using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Patient.ValueObjects;

public sealed class Governorate : ValueObject
{
    public const int MaxLength = 128;

    private Governorate(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Governorate Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Governorate is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new DomainException($"Governorate must be {MaxLength} characters or fewer.");
        }

        return new Governorate(normalized);
    }

    public static Governorate FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
