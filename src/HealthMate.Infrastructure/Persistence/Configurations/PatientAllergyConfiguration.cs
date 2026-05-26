using HealthMate.Domain.Aggregates.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.ToTable("PatientAllergies");
        builder.HasKey(allergy => allergy.Id);
        builder.Property(allergy => allergy.Substance).IsRequired();
        builder.HasIndex(allergy => new { allergy.PatientId, allergy.IsActive });

        builder.HasOne(allergy => allergy.Patient)
            .WithMany(patient => patient.Allergies)
            .HasForeignKey(allergy => allergy.PatientId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
