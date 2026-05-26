using HealthMate.Application.Common;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Application.Patients.Queries;

namespace HealthMate.Application.Patients.Handlers;

public sealed class GetPatientDashboardQueryHandler(IPatientManager patientManager)
    : IHandler<GetPatientDashboardQuery, HumanPatientMobileDashboard>
{
    public async Task<HumanPatientMobileDashboard> HandleAsync(GetPatientDashboardQuery request, CancellationToken ct)
    {
        // TODO(agent): Replace this legacy manager call when Observation/Condition dashboard data migrates to read models.
        var dashboard = await patientManager.GetMobilePatientDashboardDataAsync(request.PatientId);
        return new HumanPatientMobileDashboard
        {
            heartRate = dashboard.heartRate,
            bloodPressure = dashboard.bloodPressure,
            Hemoglobin = dashboard.Hemoglobin,
            Glucose = dashboard.Glucose,
            HighestBloodPressure = dashboard.HighestBloodPressure,
            LowestBloodPressure = dashboard.LowestBloodPressure
        };
    }
}
