using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Prescription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.HasKey(prescription => prescription.Id);
        builder.Property(prescription => prescription.Id).HasColumnName("PrescriptionId");
        builder.Property(prescription => prescription.PatientId).HasColumnName("PatientId");
        builder.Property(prescription => prescription.EncounterId).HasColumnName("EncounterId");
        builder.Property(prescription => prescription.Publisher).HasColumnName("Publisher");
        builder.Property(prescription => prescription.PrescriptionImageUrl).HasColumnName("PrescriptionImageUrl");
        builder.Property(prescription => prescription.PrescriptionDate).HasColumnName("PrescriptionDate");
        builder.Property(prescription => prescription.NameIdentifier).HasColumnName("NameIdentifier").IsRequired();

        builder.HasIndex(prescription => prescription.PatientId);
        builder.HasIndex(prescription => prescription.EncounterId);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(prescription => prescription.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Encounter>()
            .WithMany()
            .HasForeignKey(prescription => prescription.EncounterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(prescription => prescription.Medicines)
            .WithOne()
            .HasForeignKey(medicine => medicine.PrescriptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(prescription => prescription.Medicines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
