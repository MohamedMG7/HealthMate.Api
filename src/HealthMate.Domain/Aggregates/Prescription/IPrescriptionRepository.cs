namespace HealthMate.Domain.Aggregates.Prescription;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(int prescriptionId, CancellationToken ct);
    Task<Prescription?> GetByEncounterIdAsync(int encounterId, CancellationToken ct);
    Task<bool> ExistsForEncounterAsync(int encounterId, CancellationToken ct);
    Task<bool> AllMedicinesExistAsync(IReadOnlyCollection<int> medicineIds, CancellationToken ct);
    Task AddAsync(Prescription prescription, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
