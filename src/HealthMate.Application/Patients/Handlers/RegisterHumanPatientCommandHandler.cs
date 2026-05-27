using HealthMate.Application.Common;
using HealthMate.Application.Patients.Commands;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Patients.Handlers;

public sealed class RegisterHumanPatientCommandHandler(
    IPatientRepository patientRepository,
    ILogger<RegisterHumanPatientCommandHandler> logger)
    : IHandler<RegisterHumanPatientCommand, RegisterHumanPatientResult>
{
    public async Task<RegisterHumanPatientResult> HandleAsync(RegisterHumanPatientCommand request, CancellationToken ct)
    {
        var nationalId = NationalId.Create(request.NationalId);
        if (await patientRepository.ExistsByNationalIdAsync(nationalId, ct))
        {
            throw new PatientAlreadyExistsException(nationalId);
        }

        var patient = Patient.Create(
            nationalId,
            request.BirthDate,
            request.Gender,
            Governorate.Create(request.Governorate),
            City.Create(request.City),
            UserId.Create(request.ApplicationUserId),
            request.NationalIdImageUrl,
            request.Weight,
            request.Height);

        await patientRepository.AddAsync(patient, ct);
        await patientRepository.SaveChangesAsync(ct);

        logger.LogInformation("Registered patient aggregate {PatientId}", patient.Id);
        return new RegisterHumanPatientResult(patient.Id, patient.FhirId);
    }
}
