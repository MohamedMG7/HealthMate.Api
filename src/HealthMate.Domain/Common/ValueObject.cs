namespace HealthMate.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other &&
            GetType() == other.GetType() &&
            GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, static (current, component) =>
                HashCode.Combine(current, component is null ? 0 : component.GetHashCode()));
    }
}
