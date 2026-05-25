using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos
{
    public class PatientReadDto
    {
        public int Patient_Id { get; set; } // internal - incremental
        public string Patient_Fhir_Id { get; set; } = null!;
        public string NationalId { get; set; } = null!;
        public string NationalIdImageUrl { get; set; } = null!;
        public DateOnly BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string? Species { get; set; } = null!;
        public string? Breed { get; set; } = null!;
        public string Governorate { get; set; } = null!;
        public string City { get; set; } = null!;
        public bool IsVerified { get; set; }
        public string ApplicationUserId { get; set; } = null!;
        public int? OwnerId { get; set; } // FK for owner (if applicable)
    }
}
