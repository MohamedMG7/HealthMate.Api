# HealthMate.Domain

The Domain layer owns business concepts, invariants, value objects, aggregate roots, and domain exceptions.

Conventions:

- Domain types do not reference ASP.NET Core, EF Core, FHIR, or provider-specific packages.
- Aggregates expose behavior methods and keep setters private.
- Value objects validate in `Create(...)`; `FromTrusted(...)` is for persistence rehydration only.
- Repository interfaces live beside their aggregate. EF implementations live in Infrastructure.
- Domain events can be collected on `AggregateRoot<TId>`; dispatch is intentionally deferred.
