using HealthMate.Application.Common;
using HealthMate.Application.Manager.AccountManager;
using HealthMate.Application.Patients.Commands;
using HealthMate.Application.Patients.Services;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HealthMate.Application.Patients.Handlers;

public sealed class VerifyPatientCommandHandler(
    IPatientRepository patientRepository,
    IPatientAccountReader accountReader,
    IEmailService emailService,
    IDateTimeProvider clock,
    ILogger<VerifyPatientCommandHandler> logger)
    : IHandler<VerifyPatientCommand, Unit>
{
    public async Task<Unit> HandleAsync(VerifyPatientCommand request, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, ct)
            ?? throw new PatientNotFoundException(request.PatientId);

        var accounts = await accountReader.GetByUserIdsAsync([patient.ApplicationUserId], ct);
        var account = patient.ApplicationUserId is null || !accounts.TryGetValue(patient.ApplicationUserId, out var found)
            ? null
            : found;

        if (request.Approve)
        {
            if (patient.IsVerified)
            {
                throw new DomainException("Patient is already verified.");
            }

            patient.Verify(clock);
            await patientRepository.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(account?.Email))
            {
                await emailService.SendEmailAsync(account.Email, "Email Verified", "Your record is verified and ready to use.");
            }

            logger.LogInformation("Verified patient aggregate {PatientId}", patient.Patient_Id);
            return Unit.Value;
        }

        if (!string.IsNullOrWhiteSpace(account?.Email))
        {
            await emailService.SendEmailAsync(account.Email, "Email Verification Needs Your Attention", request.Reason!);
        }

        logger.LogInformation("Rejected patient verification {PatientId}", patient.Patient_Id);
        return Unit.Value;
    }
}
