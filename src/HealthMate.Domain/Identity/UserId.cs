using HealthMate.Domain.Common;

namespace HealthMate.Domain.Identity;

public sealed class UserId : ValueObject
{
    private UserId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static UserId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("User id is required.");
        }

        return new UserId(value.Trim());
    }

    public static UserId FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
