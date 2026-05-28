using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class ConditionConfiguration : IEntityTypeConfiguration<Condition>
{
    public void Configure(EntityTypeBuilder<Condition> builder)
    {
        builder.ToTable("Conditions");
        builder.HasKey(condition => condition.Id);
        builder.Property(condition => condition.Id).HasColumnName("Condition_Id");

        builder.Property(condition => condition.FhirId)
            .HasColumnName("Condition_Fhir_Id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.Property(condition => condition.PatientId).HasColumnName("PaientId");
        builder.Property(condition => condition.EncounterId).HasColumnName("EncounterId");
        builder.Property(condition => condition.DiseaseId).HasColumnName("Disease_Id");
        builder.Property(condition => condition.Severity).HasColumnName("Severity").HasConversion<int>();
        builder.Property(condition => condition.ClinicalStatus).HasColumnName("ClinicalStatus").HasConversion<int>();
        builder.Property(condition => condition.DateRecorded).HasColumnName("DateRecorded");
        builder.Property(condition => condition.Note).HasColumnName("Note");
        builder.Property<ConditionRecorder>("Recorder").HasColumnName("Recorder").HasConversion<int>();
        builder.Property<ConditionAddedByUserType>("AddedBy").HasColumnName("AddedBy").HasConversion<int>();
        builder.Property<bool>("IsOngoing").HasColumnName("isOngoing");
        builder.Property<bool>("IsChronic").HasColumnName("isChronic");
        builder.Property<int?>("BodySiteId").HasColumnName("BodySiteId");

        builder.HasIndex(condition => condition.PatientId);
        builder.HasIndex(condition => condition.EncounterId);
        builder.HasIndex(condition => condition.DiseaseId);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(condition => condition.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Encounter>()
            .WithMany()
            .HasForeignKey(condition => condition.EncounterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Disease>()
            .WithMany()
            .HasForeignKey(condition => condition.DiseaseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<BodySite>()
            .WithMany()
            .HasForeignKey("BodySiteId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
