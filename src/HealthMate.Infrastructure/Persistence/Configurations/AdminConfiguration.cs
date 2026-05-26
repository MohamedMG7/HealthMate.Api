using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.HasKey(admin => admin.Admin_Id);

        builder.HasOne(admin => admin.ApplicationUser)
            .WithOne()
            .HasForeignKey<Admin>(admin => admin.ApplicationUserId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
