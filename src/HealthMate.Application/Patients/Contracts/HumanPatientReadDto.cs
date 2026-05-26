using HealthMate.Domain.Common.Enums;

namespace HealthMate.Application.Patients.Contracts;

public class HumanPatientReadDto
{
    public int Patient_Id { get; set; }
    public string Patient_Fhir_Id { get; set; } = null!;
    public string NationalId { get; set; } = null!;
    public string NationalIdImageUrl { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string Governorate { get; set; } = null!;
    public string City { get; set; } = null!;
    public bool IsVerified { get; set; }
    public float? Weight { get; set; }
    public float? Height { get; set; }
}
