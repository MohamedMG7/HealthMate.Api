using Microsoft.AspNetCore.Http;

namespace HealthMate.Infrastructure.DTO{
    public class PrescriptionAddDto{
        //[FromForm]string medicalRecordName, [FromForm]IFormFile image, [FromForm]int patientId
        public string medicalRecordName { get; set; } = null!;
        public IFormFile image { get; set; } = null!;
        public int patientId { get; set; }
    }
}