using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class UserDiseaseExperienceConfiguration : IEntityTypeConfiguration<UserDiseaseExperience>
{
    public void Configure(EntityTypeBuilder<UserDiseaseExperience> builder)
    {
        builder.HasKey(experience => new { experience.ApplicationUserId, experience.DiseaseId });

        builder.HasOne(experience => experience.Disease)
            .WithOne()
            .HasForeignKey<UserDiseaseExperience>(experience => experience.DiseaseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(experience => experience.Experince).IsRequired(true);
    }
}
