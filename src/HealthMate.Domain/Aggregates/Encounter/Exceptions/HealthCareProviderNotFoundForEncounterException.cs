using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Encounter;

public sealed class HealthCareProviderNotFoundForEncounterException : DomainException
{
    public HealthCareProviderNotFoundForEncounterException(int healthCareProviderId)
        : base($"Health care provider '{healthCareProviderId}' was not found for encounter start.")
    {
        HealthCareProviderId = healthCareProviderId;
    }

    public int HealthCareProviderId { get; }
}
