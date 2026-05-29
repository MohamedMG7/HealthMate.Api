using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionMedicineConfiguration : IEntityTypeConfiguration<PrescriptionMedicine>
{
    public void Configure(EntityTypeBuilder<PrescriptionMedicine> builder)
    {
        builder.ToTable("PatientMedicines");
        builder.HasKey(medicine => medicine.Id);
        builder.Property(medicine => medicine.Id).HasColumnName("PatientMedicineId");
        builder.Property(medicine => medicine.PatientId).HasColumnName("PatientId");
        builder.Property(medicine => medicine.PrescriptionId).HasColumnName("PrescriptionId");
        builder.Property(medicine => medicine.MedicineId).HasColumnName("MedicineId");
        builder.Property(medicine => medicine.Dosage).HasColumnName("Dosage").IsRequired();
        builder.Property(medicine => medicine.FrequencyInHours).HasColumnName("FrequencyInHours");
        builder.Property(medicine => medicine.DurationInDays).HasColumnName("DurationInDays");
        builder.Property(medicine => medicine.AddedDate).HasColumnName("AddedDate");
        builder.Property(medicine => medicine.IsPrescribed).HasColumnName("IsPrescribed");

        builder.HasIndex(medicine => medicine.PatientId);
        builder.HasIndex(medicine => medicine.PrescriptionId);
        builder.HasIndex(medicine => medicine.MedicineId);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(medicine => medicine.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Medicine>()
            .WithMany()
            .HasForeignKey(medicine => medicine.MedicineId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
