using HealthMate.Domain.Common.Enums;

namespace HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos
{
    public class VerifiedHumanPatientReadDto
    {
        public int Patient_Id { get; set; }
        public string Patient_Fhir_Id { get; set; }
        public string NationalId { get; set; }
        public string NationalIdImageUrl { get; set; }
        public DateOnly BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string Governorate { get; set; }
        public string City { get; set; }
		public float? Weight { get; set; } //changes alot
		public float? Height { get; set; }
	}
}
