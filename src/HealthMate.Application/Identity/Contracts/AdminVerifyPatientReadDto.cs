namespace HealthMate.Application.Identity.Contracts;

public class AdminVerifyPatientReadDto
{
    public int Id { get; set; }
    public string First_Name { get; set; } = null!;
    public string Last_Name { get; set; } = null!;
    public string NatinoalIDImageUrl { get; set; } = null!;
    public string NationalIdNumber { get; set; } = null!;
    public string ApplicationUserImageUrl { get; set; } = null!;
}
