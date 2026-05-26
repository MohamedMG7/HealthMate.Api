using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Patient.ValueObjects;

public sealed class City : ValueObject
{
    public const int MaxLength = 128;

    private City(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static City Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("City is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new DomainException($"City must be {MaxLength} characters or fewer.");
        }

        return new City(normalized);
    }

    public static City FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
