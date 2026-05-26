using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Patient.ValueObjects;

public sealed class NationalId : ValueObject
{
    private NationalId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static NationalId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 14 || value.Any(static c => !char.IsDigit(c)))
        {
            throw new DomainException("National id must be exactly 14 digits.");
        }

        return new NationalId(value);
    }

    public static NationalId FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
