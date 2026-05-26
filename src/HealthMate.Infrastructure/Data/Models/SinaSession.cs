using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models;

public class SinaSession
{
    public Guid Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int HealthCareProviderId { get; set; }
    public HealthCareProvider HealthCareProvider { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime LastInteractionAt { get; set; }
    public SinaSessionStatus Status { get; set; }
    public ICollection<SinaTurn> Turns { get; set; } = new HashSet<SinaTurn>();
}
