using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.HealthRecord.Contracts{
	public class GeneralPatientInformationReadDto
	{
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string? Weight { get; set; } 
        public string? Height { get; set; }
		public string? Gender { get; set; }
	}
}
