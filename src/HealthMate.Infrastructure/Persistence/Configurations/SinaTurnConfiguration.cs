using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class SinaTurnConfiguration : IEntityTypeConfiguration<SinaTurn>
{
    public void Configure(EntityTypeBuilder<SinaTurn> builder)
    {
        builder.HasKey(turn => turn.Id);

        builder.HasOne(turn => turn.Session)
            .WithMany(session => session.Turns)
            .HasForeignKey(turn => turn.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(turn => new { turn.SessionId, turn.OrdinalIndex }).IsUnique();
        builder.Property(turn => turn.Content).IsRequired();
    }
}
