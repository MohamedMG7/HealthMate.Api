using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthMate.Infrastructure.Persistence.Configurations;

public sealed class PatientHistoryConfiguration : IEntityTypeConfiguration<PatientHistory>
{
    public void Configure(EntityTypeBuilder<PatientHistory> builder)
    {
        builder.HasKey(history => history.History_Id);
        builder.ToTable("PatientHistory");

        builder.Property(history => history.OperationType)
            .HasConversion(
                value => value == PatientHistoryOperation.Create ? "C" : value == PatientHistoryOperation.Delete ? "D" : "U",
                value => value == "C" ? PatientHistoryOperation.Create : value == "D" ? PatientHistoryOperation.Delete : PatientHistoryOperation.Update)
            .HasColumnType("character(1)");

        builder.Property(history => history.LastUpdated).HasColumnType("timestamp with time zone");
        builder.Property(history => history.RecordedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        builder.Property(history => history.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Property(history => history.RowVersion).HasConversion<long>().HasColumnType("bigint");
        builder.HasIndex(history => new { history.Patient_Fhir_Id, history.RowVersion });
        builder.HasIndex(history => new { history.Patient_Fhir_Id, history.RecordedAt });
    }
}
