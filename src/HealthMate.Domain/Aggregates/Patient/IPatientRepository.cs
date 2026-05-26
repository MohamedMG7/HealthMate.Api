using HealthMate.Domain.Aggregates.Patient.ValueObjects;

namespace HealthMate.Domain.Aggregates.Patient;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct);
    Task<Patient?> GetByFhirIdAsync(string fhirId, CancellationToken ct);
    Task<Patient?> GetByNationalIdAsync(NationalId nationalId, CancellationToken ct);
    Task<IReadOnlyList<Patient>> ListAsync(CancellationToken ct);
    Task<IReadOnlyList<Patient>> ListVerifiedAsync(CancellationToken ct);
    Task<IReadOnlyList<Patient>> ListUnverifiedAsync(CancellationToken ct);
    Task AddAsync(Patient patient, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int patientId, CancellationToken ct);
    Task<bool> ExistsByNationalIdAsync(NationalId nationalId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
