using HealthMate.Infrastructure.DTO;

namespace HealthMate.Infrastructure.Repositories{
    public interface IReporterRepo{
        Task<TrafficReportDto> getDataForTrafficReport(int healthcareproviderId);
    }
}