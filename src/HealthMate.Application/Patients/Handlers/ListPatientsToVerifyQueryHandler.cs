using HealthMate.Application.Common;
using HealthMate.Application.Identity.Contracts;
using HealthMate.Application.Patients.Queries;
using HealthMate.Application.Patients.Services;
using HealthMate.Domain.Aggregates.Patient;

namespace HealthMate.Application.Patients.Handlers;

public sealed class ListPatientsToVerifyQueryHandler(
    IPatientRepository patientRepository,
    IPatientAccountReader accountReader)
    : IHandler<ListPatientsToVerifyQuery, IReadOnlyList<AdminVerifyPatientReadDto>>
{
    public async Task<IReadOnlyList<AdminVerifyPatientReadDto>> HandleAsync(ListPatientsToVerifyQuery request, CancellationToken ct)
    {
        var patients = await patientRepository.ListUnverifiedAsync(ct);
        var accounts = await accountReader.GetByUserIdsAsync(patients.Select(patient => patient.ApplicationUserId), ct);

        return patients.Select(patient =>
        {
            PatientAccountSummary? account = null;
            var hasAccount = patient.ApplicationUserId is not null && accounts.TryGetValue(patient.ApplicationUserId, out account);
            return new AdminVerifyPatientReadDto
            {
                Id = patient.Patient_Id,
                First_Name = hasAccount ? account!.FirstName : "No Data",
                Last_Name = hasAccount ? account!.LastName : "No Data",
                NationalIdNumber = patient.NationalId.Value,
                NatinoalIDImageUrl = patient.NationalIdImageUrl,
                ApplicationUserImageUrl = hasAccount ? account!.ImageUrl ?? string.Empty : string.Empty
            };
        }).ToArray();
    }
}
