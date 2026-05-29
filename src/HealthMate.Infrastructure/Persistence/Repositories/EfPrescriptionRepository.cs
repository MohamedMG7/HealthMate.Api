using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Persistence.Repositories;

public sealed class EfPrescriptionRepository(HealthMateContext context) : IPrescriptionRepository
{
    public Task<Prescription?> GetByIdAsync(int prescriptionId, CancellationToken ct)
    {
        return context.Prescriptions
            .Include(prescription => prescription.Medicines)
            .FirstOrDefaultAsync(prescription => prescription.Id == prescriptionId, ct);
    }

    public Task<Prescription?> GetByEncounterIdAsync(int encounterId, CancellationToken ct)
    {
        return context.Prescriptions
            .Include(prescription => prescription.Medicines)
            .FirstOrDefaultAsync(prescription => prescription.EncounterId == encounterId, ct);
    }

    public Task<bool> ExistsForEncounterAsync(int encounterId, CancellationToken ct)
    {
        return context.Prescriptions.AnyAsync(prescription => prescription.EncounterId == encounterId, ct);
    }

    public async Task<bool> AllMedicinesExistAsync(IReadOnlyCollection<int> medicineIds, CancellationToken ct)
    {
        var count = await context.Medicines
            .Where(medicine => medicineIds.Contains(medicine.Id))
            .CountAsync(ct);

        return count == medicineIds.Count;
    }

    public async Task AddAsync(Prescription prescription, CancellationToken ct)
    {
        await context.Prescriptions.AddAsync(prescription, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return context.SaveChangesAsync(ct);
    }
}
