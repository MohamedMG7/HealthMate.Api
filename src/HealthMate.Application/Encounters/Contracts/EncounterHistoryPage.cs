namespace HealthMate.Application.Encounters.Contracts;

public sealed record EncounterHistoryPage(
    IReadOnlyList<EncounterHistoryItem> Items,
    int Page,
    int PageSize,
    bool HasMore);
