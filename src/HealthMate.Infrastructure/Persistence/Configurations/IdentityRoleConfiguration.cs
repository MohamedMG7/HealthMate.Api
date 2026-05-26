using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        // Static ConcurrencyStamp values keep EF's model snapshot deterministic.
        builder.HasData(
            new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "static-admin-concurrency-stamp" },
            new IdentityRole { Id = "0", Name = "Patient", NormalizedName = "PATIENT", ConcurrencyStamp = "static-patient-concurrency-stamp" },
            new IdentityRole { Id = "1", Name = "HealthCareProvider", NormalizedName = "HEALTHCAREPROVIDER", ConcurrencyStamp = "static-healthcareprovider-concurrency-stamp" });
    }
}
