using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.Patients.Contracts{
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
