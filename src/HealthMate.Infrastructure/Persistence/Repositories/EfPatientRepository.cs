using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Persistence.Repositories;

public sealed class EfPatientRepository(HealthMateContext context) : IPatientRepository
{
    public Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct)
    {
        return context.Patients.FirstOrDefaultAsync(patient => patient.Id == patientId, ct);
    }

    public Task<Patient?> GetByFhirIdAsync(string fhirId, CancellationToken ct)
    {
        return context.Patients.FirstOrDefaultAsync(patient => patient.FhirId == fhirId, ct);
    }

    public Task<Patient?> GetByNationalIdAsync(NationalId nationalId, CancellationToken ct)
    {
        return context.Patients.FirstOrDefaultAsync(patient => patient.NationalId == nationalId, ct);
    }

    public async Task<IReadOnlyList<Patient>> ListAsync(CancellationToken ct)
    {
        return await context.Patients.AsNoTracking().OrderBy(patient => patient.Id).ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<Patient>> ListVerifiedAsync(CancellationToken ct)
    {
        return await context.Patients
            .AsNoTracking()
            .Where(static patient => patient.IsVerified)
            .OrderBy(patient => patient.Id)
            .ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<Patient>> ListUnverifiedAsync(CancellationToken ct)
    {
        return await context.Patients
            .AsNoTracking()
            .Where(static patient => !patient.IsVerified)
            .OrderBy(patient => patient.Id)
            .ToArrayAsync(ct);
    }

    public async Task AddAsync(Patient patient, CancellationToken ct)
    {
        await context.Patients.AddAsync(patient, ct);
    }

    public Task<bool> ExistsByIdAsync(int patientId, CancellationToken ct)
    {
        return context.Patients.AnyAsync(patient => patient.Id == patientId, ct);
    }

    public Task<bool> ExistsByNationalIdAsync(NationalId nationalId, CancellationToken ct)
    {
        return context.Patients.AnyAsync(patient => patient.NationalId == nationalId, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return context.SaveChangesAsync(ct);
    }
}
