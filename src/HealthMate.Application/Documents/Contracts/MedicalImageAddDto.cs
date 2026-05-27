using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Application.Documents.Contracts{
    public class MedicalImageAddDto{
        [FromForm]
        public string MedicalImageName { get; set; } = null!;
		[FromForm]
		public IFormFile image { get; set; } = null!;
		[FromForm]
		public int patientId { get; set; }
		[FromForm]
		public string Interpertation { get; set; } = null!;
    }
}