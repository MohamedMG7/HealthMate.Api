using HealthMate.Infrastructure.DTO;

namespace HealthMate.Application.Manager{
    public interface IReporter{
        Task<TrafficReportDto> generateTrafficReport(int healthcareProviderId);
    }
}