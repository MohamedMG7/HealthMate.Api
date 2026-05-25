namespace HealthMate.Infrastructure.Data.Models
{
    public class PatientMedicine
    {
        public int PatientMedicineId { get; set; } // Primary key
        public int PatientId { get; set; } // Links to the patient
        public Patient Patient { get; set; } = null!;
        public int MedicineId { get; set; } // Links to the medicine
        public Medicine Medicine { get; set; } = null!;
        public int FrequencyInHours { get; set; } // How often to take the medicine (in hours)
        public int DurationInDays { get; set; } // How long to take the medicine (in days)
        public DateTime AddedDate { get; set; } = DateTime.Now; // When the medicine was assigned
        public int? PrescriptionId { get; set; } // Optional: Links to the prescription (nullable)
        public Prescription? Prescription { get; set; } // Optional: Navigation to the prescription
        public bool IsPrescribed { get; set; } // Indicates if this is part of a prescription
        public string Dosage { get; set; } = null!; // per time

    }
}