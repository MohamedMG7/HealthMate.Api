using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class LabTestResultConfiguration : IEntityTypeConfiguration<LabTestResult>
{
    public void Configure(EntityTypeBuilder<LabTestResult> builder)
    {
        builder.HasKey(result => result.Id);

        builder.HasOne(result => result.LabTest)
            .WithMany(labTest => labTest.LabTestResults)
            .HasForeignKey(result => result.LabTestId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
