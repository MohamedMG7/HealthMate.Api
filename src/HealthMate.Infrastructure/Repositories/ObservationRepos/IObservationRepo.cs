using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Ml.Contracts;
using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Patients.Contracts;

namespace HealthMate.Infrastructure.Repositories.ObservationRepos{
    public interface IObservationRepo : IGenericRepository<Observation>{
        Task<List<HeartRateValueAndDateDto>> GetHeartRateReadingsInXTime(int patientId, int periodInDays);
        Task<List<HemoglobinValueAndDateDto>> GetHemoglobinDataInXTime(int patientId, int periodInDays);
        Task<List<GlucoseLevelValueAndDateDto>> GetGlucoseReadingsInXTime(int patientId, int periodInDays);
        Task<List<DocumentDto>> GetMostRecentDocuments(int patientId);
        Task<List<bloodPressureValueAndDateDto>> GetBloodPressureReadingsInXTime(int patientId, int periodInDays);
        Task<string> GetHighestBloodPressureAsync(int patientId, int periodInDays);
        Task<string> GetLowestBloodPressureAsync(int patientId, int periodInDays);
        Task<AnimeaMLDto> GetRecentCBCTestForML(int patientId);
        Task<decimal> GetLastGlucoseReading(int patientId);

    }
}