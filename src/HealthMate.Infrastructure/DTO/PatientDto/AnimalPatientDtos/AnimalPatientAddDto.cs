using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.DTO.PatientDto.AnimalPatientDtos
{
	public class AnimalPatientAddDto
	{
		public DateOnly BirthDate { get; set; }
        public string Name { get; set; }
        public Gender Gender { get; set; }
		public string? Species { get; set; } 
		public string? Breed { get; set; }
        public int Owner_Id { get; set; }
	}
}
