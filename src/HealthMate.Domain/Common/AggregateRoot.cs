namespace HealthMate.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => domainEvents.Clear();
}
