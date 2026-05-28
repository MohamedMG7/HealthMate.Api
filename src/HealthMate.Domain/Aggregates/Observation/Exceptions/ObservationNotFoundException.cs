using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Observation;

public sealed class ObservationNotFoundException : DomainException
{
    public ObservationNotFoundException(int observationId)
        : base($"Observation '{observationId}' was not found.")
    {
        ObservationId = observationId;
    }

    public int ObservationId { get; }
}
