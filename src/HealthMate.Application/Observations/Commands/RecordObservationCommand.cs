using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Observation;

namespace HealthMate.Application.Observations.Commands;

public sealed record RecordObservationCommand(
    int EncounterId,
    ObservationCategory Category,
    string? Code,
    string? CodeDisplayName,
    decimal? ValueQuantity,
    string? ValueUnit,
    string? Interpretation,
    int? BodySiteId,
    DateTime DateOfObservation,
    string? NameIdentifier) : ICommand<RecordObservationResult>;

public sealed record RecordObservationResult(int ObservationId, string ObservationFhirId, int PatientId);
