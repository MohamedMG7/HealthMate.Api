using HealthMate.Application.Common;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Application.Patients.Queries;
using HealthMate.Domain.Aggregates.Patient;

namespace HealthMate.Application.Patients.Handlers;

public sealed class ListPatientsQueryHandler(IPatientRepository patientRepository)
    : IHandler<ListPatientsQuery, IReadOnlyList<HumanPatientReadDto>>
{
    public async Task<IReadOnlyList<HumanPatientReadDto>> HandleAsync(ListPatientsQuery request, CancellationToken ct)
    {
        var patients = await patientRepository.ListAsync(ct);
        return patients.Select(PatientContractMapper.ToHumanPatientReadDto).ToArray();
    }
}
