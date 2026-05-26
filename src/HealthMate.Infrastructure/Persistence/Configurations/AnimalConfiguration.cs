using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.HasKey(animal => animal.Animal_Id);

        builder.Property(animal => animal.Animal_Fhir_Id)
            .IsRequired(true)
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.HasOne(animal => animal.Patient)
            .WithMany()
            .HasForeignKey(animal => animal.Owner_Id)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
