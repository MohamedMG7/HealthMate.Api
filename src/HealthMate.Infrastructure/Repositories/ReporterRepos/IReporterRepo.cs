using HealthMate.Application.Admin.Contracts;

namespace HealthMate.Infrastructure.Repositories{
    public interface IReporterRepo{
        Task<TrafficReportDto> getDataForTrafficReport(int healthcareproviderId);
    }
}
