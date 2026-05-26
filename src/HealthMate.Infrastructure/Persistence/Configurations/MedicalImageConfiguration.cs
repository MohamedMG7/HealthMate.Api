using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class MedicalImageConfiguration : IEntityTypeConfiguration<MedicalImage>
{
    public void Configure(EntityTypeBuilder<MedicalImage> builder)
    {
        builder.HasKey(image => image.MedicalImageId);

        builder.HasOne(image => image.patient)
            .WithMany()
            .HasForeignKey(image => image.paitentId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
