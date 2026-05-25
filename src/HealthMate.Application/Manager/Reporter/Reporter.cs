using HealthMate.Infrastructure.DTO;
using HealthMate.Infrastructure.Repositories;

namespace HealthMate.Application.Manager{
    public class Reporter : IReporter{
        private readonly IReporterRepo _reporterRepo;
        public Reporter(IReporterRepo reporterRepo)
        {
            _reporterRepo = reporterRepo;
        }

        public async Task<TrafficReportDto> generateTrafficReport(int healthcareProviderId){
            var reportData = await _reporterRepo.getDataForTrafficReport(healthcareProviderId);

            return reportData;
        }
    }
}