using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(user => user.Id);

        builder.HasMany(user => user.UserFeedbacks)
            .WithOne(feedback => feedback.ApplicationUser)
            .HasForeignKey(feedback => feedback.ApplicationUser_Id)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(user => user.UserDiseaseExperiences)
            .WithOne(experience => experience.ApplicationUser)
            .HasForeignKey(experience => experience.ApplicationUserId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
