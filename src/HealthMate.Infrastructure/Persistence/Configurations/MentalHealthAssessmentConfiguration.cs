using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class MentalHealthAssessmentConfiguration : IEntityTypeConfiguration<MentalHealthAssessment>
{
    public void Configure(EntityTypeBuilder<MentalHealthAssessment> builder)
    {
        builder.HasKey(assessment => assessment.Id);

        builder.HasOne(assessment => assessment.Patient)
            .WithMany()
            .HasForeignKey(assessment => assessment.patientId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
