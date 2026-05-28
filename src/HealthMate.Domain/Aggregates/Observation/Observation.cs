using HealthMate.Domain.Common;

namespace HealthMate.Domain.Aggregates.Observation;

public sealed class Observation : AggregateRoot<int>
{
    private const int GeneratedIdentifierHexLength = 8;

    private Observation()
    {
    }

    public string FhirId { get; private set; } = null!;
    public int PatientId { get; private set; }
    public int? EncounterId { get; private set; }
    public ObservationCategory Category { get; private set; }
    public string? Code { get; private set; }
    public string? CodeDisplayName { get; private set; }
    public decimal? ValueQuantity { get; private set; }
    public string? ValueUnit { get; private set; }
    public string? Interpretation { get; private set; }
    public int? BodySiteId { get; private set; }
    public DateTime DateOfObservation { get; private set; }
    public string NameIdentifier { get; private set; } = null!;
    public bool IsDeleted { get; private set; }

    public static Observation Record(
        int patientId,
        int? encounterId,
        ObservationCategory category,
        string? code,
        string? codeDisplayName,
        decimal? valueQuantity,
        string? valueUnit,
        string? interpretation,
        int? bodySiteId,
        DateTime dateOfObservation,
        string? nameIdentifier,
        IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (patientId <= 0)
        {
            throw new DomainException("Patient id must be greater than zero.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new DomainException("Observation category must be a defined value.");
        }

        if (dateOfObservation > clock.UtcNow.UtcDateTime.AddMinutes(1))
        {
            throw new DomainException("Date of observation cannot be in the future.");
        }

        if (valueQuantity.HasValue && valueQuantity <= 0)
        {
            throw new DomainException("Value quantity must be greater than zero.");
        }

        var normalizedCode = NormalizeOptionalText(code, "Code");
        var normalizedCodeDisplayName = NormalizeOptionalText(codeDisplayName, "Code display name");
        var normalizedValueUnit = NormalizeOptionalText(valueUnit, "Value unit");
        var normalizedInterpretation = string.IsNullOrWhiteSpace(interpretation) ? null : interpretation.Trim();
        var normalizedNameIdentifier = string.IsNullOrWhiteSpace(nameIdentifier)
            ? GenerateNameIdentifier(normalizedCode)
            : nameIdentifier.Trim();

        return new Observation
        {
            PatientId = patientId,
            EncounterId = encounterId,
            Category = category,
            Code = normalizedCode,
            CodeDisplayName = normalizedCodeDisplayName,
            ValueQuantity = valueQuantity,
            ValueUnit = normalizedValueUnit,
            Interpretation = normalizedInterpretation,
            BodySiteId = bodySiteId,
            DateOfObservation = dateOfObservation,
            NameIdentifier = normalizedNameIdentifier,
            IsDeleted = false
        };
    }

    private static string? NormalizeOptionalText(string? value, string fieldName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} must not be empty.");
        }

        return value.Trim();
    }

    private static string GenerateNameIdentifier(string? code)
    {
        var codeSegment = string.IsNullOrWhiteSpace(code) ? "general" : code;
        var suffix = Guid.NewGuid().ToString("N")[..GeneratedIdentifierHexLength];
        return $"OBS-{codeSegment}-{suffix}";
    }
}
