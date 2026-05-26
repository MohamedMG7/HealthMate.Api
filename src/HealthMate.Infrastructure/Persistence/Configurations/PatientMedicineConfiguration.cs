using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PatientMedicineConfiguration : IEntityTypeConfiguration<PatientMedicine>
{
    public void Configure(EntityTypeBuilder<PatientMedicine> builder)
    {
        builder.HasKey(patientMedicine => patientMedicine.PatientMedicineId);

        builder.HasOne(patientMedicine => patientMedicine.Patient)
            .WithMany()
            .HasForeignKey(patientMedicine => patientMedicine.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(patientMedicine => patientMedicine.Medicine)
            .WithMany(medicine => medicine.PatientMedicines)
            .HasForeignKey(patientMedicine => patientMedicine.MedicineId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
