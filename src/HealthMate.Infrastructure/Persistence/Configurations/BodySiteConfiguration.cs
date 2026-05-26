using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class BodySiteConfiguration : IEntityTypeConfiguration<BodySite>
{
    public void Configure(EntityTypeBuilder<BodySite> builder)
    {
        builder.HasKey(bodySite => bodySite.BodySite_Id);
    }
}
