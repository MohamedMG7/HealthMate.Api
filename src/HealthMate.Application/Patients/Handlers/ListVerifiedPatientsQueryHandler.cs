using HealthMate.Application.Common;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Application.Patients.Queries;
using HealthMate.Domain.Aggregates.Patient;

namespace HealthMate.Application.Patients.Handlers;

public sealed class ListVerifiedPatientsQueryHandler(IPatientRepository patientRepository)
    : IHandler<ListVerifiedPatientsQuery, IReadOnlyList<VerifiedHumanPatientReadDto>>
{
    public async Task<IReadOnlyList<VerifiedHumanPatientReadDto>> HandleAsync(ListVerifiedPatientsQuery request, CancellationToken ct)
    {
        var patients = await patientRepository.ListVerifiedAsync(ct);
        return patients.Select(PatientContractMapper.ToVerifiedHumanPatientReadDto).ToArray();
    }
}
