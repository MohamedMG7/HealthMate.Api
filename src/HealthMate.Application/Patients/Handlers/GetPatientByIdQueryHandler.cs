using HealthMate.Application.Common;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Application.Patients.Queries;
using HealthMate.Domain.Aggregates.Patient;

namespace HealthMate.Application.Patients.Handlers;

public sealed class GetPatientByIdQueryHandler(IPatientRepository patientRepository)
    : IHandler<GetPatientByIdQuery, HumanPatientReadDto?>
{
    public async Task<HumanPatientReadDto?> HandleAsync(GetPatientByIdQuery request, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, ct);
        return patient is null ? null : PatientContractMapper.ToHumanPatientReadDto(patient);
    }
}
