using HealthMate.Application.Common;
using HealthMate.Application.Identity.Contracts;

namespace HealthMate.Application.Patients.Queries;

public sealed record ListPatientsToVerifyQuery : IQuery<IReadOnlyList<AdminVerifyPatientReadDto>>;
