using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class LabTestAttributeConfiguration : IEntityTypeConfiguration<LabTestAttribute>
{
    public void Configure(EntityTypeBuilder<LabTestAttribute> builder)
    {
        builder.HasKey(attribute => attribute.Id);

        builder.HasMany(attribute => attribute.LabTestResults)
            .WithOne(result => result.LabTestAttribute)
            .HasForeignKey(result => result.LabTestAttributeId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
