using HealthMate.Domain.Aggregates.Observation;

namespace HealthMate.Application.Observations.Contracts;

public sealed record RecordObservationRequestDto(
    ObservationCategory Category,
    string? Code,
    string? CodeDisplayName,
    decimal? ValueQuantity,
    string? ValueUnit,
    string? Interpretation,
    int? BodySiteId,
    DateTime DateOfObservation,
    string? NameIdentifier);
