using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class ConditionConfiguration : IEntityTypeConfiguration<Condition>
{
    public void Configure(EntityTypeBuilder<Condition> builder)
    {
        builder.HasKey(condition => condition.Condition_Id);

        builder.Property(condition => condition.Condition_Fhir_Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.HasOne(condition => condition.Patient)
            .WithMany()
            .HasForeignKey(condition => condition.PaientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(condition => condition.BodySite)
            .WithMany(bodySite => bodySite.Conditions)
            .HasForeignKey(condition => condition.BodySiteId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(condition => condition.Disease)
            .WithMany(disease => disease.Conditions)
            .HasForeignKey(condition => condition.Disease_Id)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
