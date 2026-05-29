using HealthMate.Application.Common;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Prescriptions.Commands;

public sealed class WritePrescriptionCommandHandler(
    IEncounterRepository encounterRepository,
    IPrescriptionRepository prescriptionRepository,
    IDateTimeProvider clock,
    ILogger<WritePrescriptionCommandHandler> logger)
    : IHandler<WritePrescriptionCommand, WritePrescriptionResult>
{
    public async Task<WritePrescriptionResult> HandleAsync(WritePrescriptionCommand request, CancellationToken ct)
    {
        var encounter = await encounterRepository.GetByIdAsync(request.EncounterId, ct);
        if (encounter is null)
        {
            throw new EncounterNotFoundException(request.EncounterId);
        }

        if (encounter.Status != EncounterStatus.Active)
        {
            logger.LogWarning(
                "Late entry: recording prescription on {Status} encounter {EncounterId} for patient {PatientId}",
                encounter.Status,
                encounter.Id,
                encounter.PatientId);
        }

        if (await prescriptionRepository.ExistsForEncounterAsync(request.EncounterId, ct))
        {
            throw new PrescriptionAlreadyExistsForEncounterException(request.EncounterId);
        }

        var medicineIds = request.Medicines.Select(medicine => medicine.MedicineId).Distinct().ToArray();
        if (!await prescriptionRepository.AllMedicinesExistAsync(medicineIds, ct))
        {
            throw new MedicineNotFoundForPrescriptionException(medicineIds);
        }

        var lines = request.Medicines
            .Select(medicine => new PrescriptionMedicineLine(
                medicine.MedicineId,
                medicine.Dosage,
                medicine.FrequencyInHours,
                medicine.DurationInDays))
            .ToArray();

        var prescription = Prescription.Write(
            encounter.PatientId,
            encounter.Id,
            request.Publisher,
            lines,
            clock);

        await prescriptionRepository.AddAsync(prescription, ct);
        await prescriptionRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Wrote prescription {PrescriptionId} on encounter {EncounterId} for patient {PatientId} with {MedicineCount} medicines",
            prescription.Id,
            encounter.Id,
            encounter.PatientId,
            prescription.Medicines.Count);

        return new WritePrescriptionResult(prescription.Id, encounter.PatientId, prescription.Medicines.Count);
    }
}
