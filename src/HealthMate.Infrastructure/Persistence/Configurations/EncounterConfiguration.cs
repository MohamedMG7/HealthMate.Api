using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Encounter.ValueObjects;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("Encounters");
        builder.HasKey(encounter => encounter.Id);
        builder.Property(encounter => encounter.Id).HasColumnName("Encounter_Id");

        builder.Property(encounter => encounter.FhirId)
            .HasColumnName("Encounter_Fhir_Id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.Property(encounter => encounter.PatientId).HasColumnName("PatientId");
        builder.Property(encounter => encounter.HealthCareProviderId).HasColumnName("HealthCareProviderId");
        builder.Property(encounter => encounter.StartDate).HasColumnName("StartDate");
        builder.Property(encounter => encounter.EndDate).HasColumnName("EndDate");
        builder.Property(encounter => encounter.Location).HasColumnName("Location");
        builder.Property(encounter => encounter.ReasonToVisit)
            .HasColumnName("Reason_To_Visit")
            .HasConversion(reason => reason.Value, value => ReasonToVisit.FromTrusted(value))
            .IsRequired();
        builder.Property(encounter => encounter.TreatmentPlan)
            .HasColumnName("Treatment_Plan")
            .IsRequired();
        builder.Property(encounter => encounter.Note).HasColumnName("Note");
        builder.Property(encounter => encounter.IsDeleted).HasColumnName("isDeleted");
        builder.Property(encounter => encounter.Status)
            .HasColumnName("EncounterStatus")
            .HasConversion<int>()
            .HasDefaultValue(EncounterStatus.Active);

        builder.HasIndex(encounter => encounter.PatientId);
        builder.HasIndex(encounter => encounter.HealthCareProviderId);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(encounter => encounter.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<HealthCareProvider>()
            .WithMany(provider => provider.Encounters)
            .HasForeignKey(encounter => encounter.HealthCareProviderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
