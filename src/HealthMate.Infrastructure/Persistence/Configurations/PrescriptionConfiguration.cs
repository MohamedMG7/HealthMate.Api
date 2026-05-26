using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.HasOne(prescription => prescription.Encounter)
            .WithMany(encounter => encounter.Prescriptions)
            .HasForeignKey(prescription => prescription.EncounterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
