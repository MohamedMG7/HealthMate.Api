using HealthMate.Domain.Common.Enums;

namespace HealthMate.Infrastructure.Data.Models;

public class PatientHistory
{
    public long History_Id { get; set; }
    public int Patient_Id { get; set; }
    public string Patient_Fhir_Id { get; set; } = null!;
    public string NationalId { get; set; } = null!;
    public string NationalIdImageUrl { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string Governorate { get; set; } = null!;
    public string City { get; set; } = null!;
    public bool IsVerified { get; set; }
    public string? ApplicationUserId { get; set; }
    public string? Name { get; set; }
    public string? PhoneE164 { get; set; }
    public string? Email { get; set; }
    public float? Weight { get; set; }
    public float? Height { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public uint RowVersion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public PatientHistoryOperation OperationType { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public enum PatientHistoryOperation
{
    Create,
    Update,
    Delete
}
