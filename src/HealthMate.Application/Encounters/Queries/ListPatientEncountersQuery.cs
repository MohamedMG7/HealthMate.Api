using HealthMate.Application.Common;
using HealthMate.Application.Encounters.Contracts;

namespace HealthMate.Application.Encounters.Queries;

public sealed record ListPatientEncountersQuery(
    int PatientId, int Page, int PageSize) : IQuery<EncounterHistoryPage>;
