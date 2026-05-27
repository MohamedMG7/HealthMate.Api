using HealthMate.Application.Admin.Contracts;

namespace HealthMate.Application.Manager{
    public interface IReporter{
        Task<TrafficReportDto> generateTrafficReport(int healthcareProviderId);
    }
}
