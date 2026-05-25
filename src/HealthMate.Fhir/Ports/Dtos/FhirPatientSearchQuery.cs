namespace HealthMate.Fhir.Ports.Dtos;

public sealed record FhirPatientSearchQuery(
    IReadOnlyList<string> Ids,
    IReadOnlyList<FhirDateTimeFilter> LastUpdated,
    FhirStringSearch? Name,
    FhirTokenSearch? Identifier,
    IReadOnlyList<FhirDateFilter> BirthDate,
    string? Gender,
    IReadOnlyList<FhirSort> Sorts,
    int Count,
    int Offset)
{
    public static FhirPatientSearchQuery Empty { get; } = new(
        [],
        [],
        null,
        null,
        [],
        null,
        [new FhirSort("_lastUpdated", true)],
        Count: 20,
        Offset: 0);
}

public enum FhirSearchPrefix
{
    Eq,
    Gt,
    Lt,
    Ge,
    Le
}

public sealed record FhirDateTimeFilter(FhirSearchPrefix Prefix, DateTimeOffset Value);

public sealed record FhirDateFilter(FhirSearchPrefix Prefix, DateOnly Value);

public sealed record FhirStringSearch(string Value, bool Exact);

public sealed record FhirTokenSearch(string? System, string Value);

public sealed record FhirSort(string Field, bool Descending);
