using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.HasKey(labTest => labTest.LabTestId);

        builder.HasOne(labTest => labTest.patient)
            .WithMany()
            .HasForeignKey(labTest => labTest.patientId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
