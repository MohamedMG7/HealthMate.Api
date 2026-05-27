using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Patients.Contracts;

namespace HealthMate.Application.Patients.Contracts;

public class HumanPatientMobileDashboard
{
    public HeartRateDto heartRate { get; set; } = null!;
    public bloodPressureDto bloodPressure { get; set; } = null!;
    public HemoglobinDto Hemoglobin { get; set; } = null!;
    public GlucoseLevelDto Glucose { get; set; } = null!;
    public string HighestBloodPressure { get; set; } = null!;
    public string LowestBloodPressure { get; set; } = null!;
}
