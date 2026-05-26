using HealthMate.Domain.Aggregates.Patient;
using Microsoft.AspNetCore.Http;

namespace HealthMate.Infrastructure.Data.Models{
    public class Prescription{
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public string? Publisher { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string? PrescriptionImageUrl { get; set; }
        public int? EncounterId { get; set; }
        public string NameIdentifier { get; set; } = null!; // this is used as identifier to sina
        public ICollection<PatientMedicine>? PatientMedicines { get; set; } = new HashSet<PatientMedicine>();
        public Encounter? Encounter { get; set; }
    }
}
