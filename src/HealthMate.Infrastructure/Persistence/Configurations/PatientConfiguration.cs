using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(patient => patient.Id);
        builder.Property(patient => patient.Id).HasColumnName("Patient_Id");

        builder.Property(patient => patient.FhirId)
            .HasColumnName("Patient_Fhir_Id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.Property(patient => patient.NationalId)
            .HasConversion(id => id.Value, value => NationalId.FromTrusted(value))
            .IsRequired();

        builder.Property(patient => patient.Governorate)
            .HasConversion(governorate => governorate.Value, value => Governorate.FromTrusted(value))
            .IsRequired();

        builder.Property(patient => patient.City)
            .HasConversion(city => city.Value, value => City.FromTrusted(value))
            .IsRequired();

        builder.Property(patient => patient.LastUpdated)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(patient => patient.RowVersion)
            .HasConversion<long>()
            .HasColumnType("bigint")
            .HasDefaultValue(1L)
            .IsConcurrencyToken();

        builder.Property(patient => patient.IsDeleted).HasDefaultValue(false);
        builder.Property(patient => patient.DeletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(patient => patient.FhirId)
            .HasDatabaseName("IX_Patients_Patient_Fhir_Id_Active")
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Patient>(patient => patient.ApplicationUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(patient => patient.Allergies)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
