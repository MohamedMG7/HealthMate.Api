using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.HasKey(attachment => attachment.Id);

        builder.HasOne(attachment => attachment.Message)
            .WithMany(message => message.Attachments)
            .HasForeignKey(attachment => attachment.MessageId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(attachment => attachment.AttatchmentType).IsRequired(true);
        builder.Property(attachment => attachment.AttatchmentId).IsRequired(true);
    }
}
