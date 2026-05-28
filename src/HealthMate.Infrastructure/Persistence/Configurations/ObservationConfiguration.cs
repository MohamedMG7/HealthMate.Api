using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("Observations");
        builder.HasKey(observation => observation.Id);
        builder.Property(observation => observation.Id).HasColumnName("Observation_Id");

        builder.Property(observation => observation.FhirId)
            .HasColumnName("Observation_Fhir_Id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.Property(observation => observation.PatientId).HasColumnName("PatientId");
        builder.Property(observation => observation.EncounterId).HasColumnName("EncounterId");
        builder.Property(observation => observation.Category).HasColumnName("Category").HasConversion<int>();
        builder.Property(observation => observation.Code).HasColumnName("Code");
        builder.Property(observation => observation.CodeDisplayName).HasColumnName("CodeDisplayName");
        builder.Property(observation => observation.ValueQuantity)
            .HasColumnName("ValueQuanitity")
            .HasColumnType("numeric(18,3)");
        builder.Property(observation => observation.ValueUnit).HasColumnName("ValueUnit");
        builder.Property(observation => observation.Interpretation).HasColumnName("Interpertation");
        builder.Property(observation => observation.BodySiteId).HasColumnName("BodySiteId");
        builder.Property(observation => observation.DateOfObservation).HasColumnName("DateOfObservation");
        builder.Property(observation => observation.NameIdentifier).HasColumnName("NameIdentifier").IsRequired();
        builder.Property(observation => observation.IsDeleted).HasColumnName("isDeleted");

        builder.HasIndex(observation => observation.PatientId);
        builder.HasIndex(observation => observation.EncounterId);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(observation => observation.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Encounter>()
            .WithMany()
            .HasForeignKey(observation => observation.EncounterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<BodySite>()
            .WithOne()
            .HasForeignKey<Observation>(observation => observation.BodySiteId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
