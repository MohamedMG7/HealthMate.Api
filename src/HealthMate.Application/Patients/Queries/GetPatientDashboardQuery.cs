using HealthMate.Application.Common;
using HealthMate.Application.Patients.Contracts;

namespace HealthMate.Application.Patients.Queries;

public sealed record GetPatientDashboardQuery(int PatientId) : IQuery<HumanPatientMobileDashboard>;
