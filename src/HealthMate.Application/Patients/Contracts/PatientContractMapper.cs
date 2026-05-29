using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace HealthMate.Application.Patients.Contracts;

[Mapper]
internal static partial class PatientContractMapper
{
    [MapProperty(nameof(Patient.Id), nameof(HumanPatientReadDto.Patient_Id))]
    [MapProperty(nameof(Patient.FhirId), nameof(HumanPatientReadDto.Patient_Fhir_Id))]
    public static partial HumanPatientReadDto ToHumanPatientReadDto(Patient patient);

    [MapProperty(nameof(Patient.Id), nameof(VerifiedHumanPatientReadDto.Patient_Id))]
    [MapProperty(nameof(Patient.FhirId), nameof(VerifiedHumanPatientReadDto.Patient_Fhir_Id))]
    public static partial VerifiedHumanPatientReadDto ToVerifiedHumanPatientReadDto(Patient patient);

    private static string MapNationalId(NationalId value) => value.Value;
    private static string MapGovernorate(Governorate value) => value.Value;
    private static string MapCity(City value) => value.Value;
}
