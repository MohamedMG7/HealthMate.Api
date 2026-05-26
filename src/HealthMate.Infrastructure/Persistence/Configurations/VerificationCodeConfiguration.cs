using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.HasKey(code => new { code.ApplicationUser_Id, code.VerificationCodeDigits });

        builder.HasOne(code => code.ApplicationUser)
            .WithOne()
            .HasForeignKey<VerificationCode>(code => code.ApplicationUser_Id);

        builder.Property(code => code.VerificationCodeDigits).IsRequired(true);
    }
}
