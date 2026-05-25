using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Repositories.PatientAllergyRepos;

public interface IPatientAllergyRepo : IGenericRepository<PatientAllergy>
{
    Task<IReadOnlyList<PatientAllergy>> GetActiveByPatientAsync(int patientId, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}
