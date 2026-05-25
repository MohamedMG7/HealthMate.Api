using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models;

public class PatientAllergy
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string Substance { get; set; } = null!;
    public AllergySeverity Severity { get; set; }
    public string? Reaction { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
