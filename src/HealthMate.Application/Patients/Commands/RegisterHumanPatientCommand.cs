using HealthMate.Application.Common;
using HealthMate.Domain.Common.Enums;

namespace HealthMate.Application.Patients.Commands;

public sealed record RegisterHumanPatientCommand(
    string NationalId,
    string NationalIdImageUrl,
    DateOnly BirthDate,
    Gender Gender,
    string Governorate,
    string City,
    string ApplicationUserId,
    float? Weight,
    float? Height) : ICommand<RegisterHumanPatientResult>;

public sealed record RegisterHumanPatientResult(int PatientId, string PatientFhirId);
