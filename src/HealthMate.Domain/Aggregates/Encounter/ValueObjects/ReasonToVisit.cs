using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Encounter.ValueObjects;

public sealed class ReasonToVisit : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 500;

    private ReasonToVisit(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ReasonToVisit Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Reason to visit is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < MinLength)
        {
            throw new DomainException($"Reason to visit must be at least {MinLength} characters.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Reason to visit must be at most {MaxLength} characters.");
        }

        return new ReasonToVisit(trimmed);
    }

    public static ReasonToVisit FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
