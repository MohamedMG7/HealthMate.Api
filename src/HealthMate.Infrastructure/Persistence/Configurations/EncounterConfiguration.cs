using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.HasKey(encounter => encounter.Encounter_Id);

        builder.Property(encounter => encounter.Encounter_Fhir_Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.HasOne(encounter => encounter.Patient)
            .WithMany()
            .HasForeignKey(encounter => encounter.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(encounter => encounter.HealthCareProvider)
            .WithMany(provider => provider.Encounters)
            .HasForeignKey(encounter => encounter.HealthCareProviderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(encounter => encounter.Conditions)
            .WithOne(condition => condition.Encounter)
            .HasForeignKey(condition => condition.EncounterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
