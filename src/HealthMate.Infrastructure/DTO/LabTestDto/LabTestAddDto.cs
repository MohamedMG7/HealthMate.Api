using HealthMate.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Http;

namespace HealthMate.Infrastructure.DTO.LabTestDto{
    public class LabTestAddDto{
        public int patientId { get; set; }
        public string LabTestName { get; set; } = null!;
		public ICollection<LabTestResultDto> LabTestResult { get; set; } = new HashSet<LabTestResultDto>();
    }

	public class LabTestImageDto
	{
        public int patientId { get; set; }
        public string LabTestName { get; set; } = null!;
		public IFormFile? Image { get; set; }
	}


	public class LabTestResultDto{
        public string AttributeName { get; set; } = null!;
        public decimal Value { get; set; }
        
    }
}