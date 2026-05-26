using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.HasKey(observation => observation.Observation_Id);

        builder.Property(observation => observation.Observation_Fhir_Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.HasOne(observation => observation.Patient)
            .WithMany()
            .HasForeignKey(observation => observation.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(observation => observation.BodySite)
            .WithOne()
            .HasForeignKey<Observation>(observation => observation.BodySiteId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(observation => observation.ValueQuanitity).HasColumnType("numeric(18,3)");
    }
}
