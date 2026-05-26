using HealthMate.Application.Common;

namespace HealthMate.Application.Patients.Commands;

public sealed record VerifyPatientCommand(int PatientId, bool Approve, string? Reason) : ICommand<Unit>;
