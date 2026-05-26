using HealthMate.Domain.Common.Enums;

namespace HealthMate.Application.Patients.Contracts;

public class HumanPatientAddDto
{
    public string NationalId { get; set; } = null!;
    public string NationalIdImageUrl { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string Governorate { get; set; } = null!;
    public string City { get; set; } = null!;
    public string ApplicationUserId { get; set; } = null!;
    public float? Weight { get; set; }
    public float? Height { get; set; }
}
