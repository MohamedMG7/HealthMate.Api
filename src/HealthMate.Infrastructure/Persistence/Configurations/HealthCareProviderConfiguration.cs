using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class HealthCareProviderConfiguration : IEntityTypeConfiguration<HealthCareProvider>
{
    public void Configure(EntityTypeBuilder<HealthCareProvider> builder)
    {
        builder.HasKey(provider => provider.HealthCareProvider_Id);

        builder.HasOne(provider => provider.ApplicationUser)
            .WithOne()
            .HasForeignKey<HealthCareProvider>(provider => provider.ApplicationUserId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
