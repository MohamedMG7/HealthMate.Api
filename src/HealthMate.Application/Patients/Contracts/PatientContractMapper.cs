using HealthMate.Domain.Aggregates.Patient;

namespace HealthMate.Application.Patients.Contracts;

internal static class PatientContractMapper
{
    public static HumanPatientReadDto ToHumanPatientReadDto(Patient patient)
    {
        return new HumanPatientReadDto
        {
            Patient_Id = patient.Patient_Id,
            Patient_Fhir_Id = patient.Patient_Fhir_Id,
            NationalId = patient.NationalId.Value,
            NationalIdImageUrl = patient.NationalIdImageUrl,
            BirthDate = patient.BirthDate,
            Gender = patient.Gender,
            Governorate = patient.Governorate.Value,
            City = patient.City.Value,
            IsVerified = patient.IsVerified,
            Weight = patient.Weight,
            Height = patient.Height
        };
    }

    public static VerifiedHumanPatientReadDto ToVerifiedHumanPatientReadDto(Patient patient)
    {
        return new VerifiedHumanPatientReadDto
        {
            Patient_Id = patient.Patient_Id,
            Patient_Fhir_Id = patient.Patient_Fhir_Id,
            NationalId = patient.NationalId.Value,
            NationalIdImageUrl = patient.NationalIdImageUrl,
            BirthDate = patient.BirthDate,
            Gender = patient.Gender,
            Governorate = patient.Governorate.Value,
            City = patient.City.Value,
            Weight = patient.Weight,
            Height = patient.Height
        };
    }
}
