using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class SinaSessionConfiguration : IEntityTypeConfiguration<SinaSession>
{
    public void Configure(EntityTypeBuilder<SinaSession> builder)
    {
        builder.HasKey(session => session.Id);

        builder.HasOne(session => session.Patient)
            .WithMany()
            .HasForeignKey(session => session.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(session => session.HealthCareProvider)
            .WithMany()
            .HasForeignKey(session => session.HealthCareProviderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(session => new { session.PatientId, session.HealthCareProviderId, session.Status });
    }
}
