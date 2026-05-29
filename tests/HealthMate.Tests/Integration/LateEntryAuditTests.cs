using System.Collections.Concurrent;
using System.Net;
using FluentAssertions;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthMate.Tests.Integration;

public sealed class LateEntryAuditTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task Late_entries_on_finished_encounter_succeed_and_emit_warnings()
    {
        var seeded = await EncounterIntegrationTestHelpers.SeedFinishedEncounterWithCatalogAsync(
            fixture.Factory.Services,
            "late-entry-audit");
        using var logs = new CollectingLoggerProvider();
        using var factory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
        });
        using var client = factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, seeded.ProviderId);

        var observation = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/observations",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                category = ObservationCategory.VitalSigns,
                code = "hr",
                codeDisplayName = "heartrate",
                valueQuantity = 72m,
                valueUnit = "bpm",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = DateTime.UtcNow.AddMinutes(-5),
                nameIdentifier = (string?)null
            }));
        var condition = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/conditions",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                diseaseId = seeded.DiseaseId,
                severity = Severity.Mild,
                clinicalStatus = ClinicalStatus.Active,
                dateRecorded = DateTime.UtcNow.AddMinutes(-5),
                note = (string?)null
            }));
        var prescription = await client.PostAsync(
            $"/api/Encounter/{seeded.EncounterId}/prescription",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId = seeded.MedicineId, dosage = "10mg", frequencyInHours = 8, durationInDays = 5 }
                }
            }));

        observation.StatusCode.Should().Be(HttpStatusCode.Created);
        condition.StatusCode.Should().Be(HttpStatusCode.Created);
        prescription.StatusCode.Should().Be(HttpStatusCode.Created);
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Late entry: recording observation on Finished encounter", StringComparison.Ordinal));
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Late entry: recording condition on Finished encounter", StringComparison.Ordinal));
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Late entry: recording prescription on Finished encounter", StringComparison.Ordinal));
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> entries = new();

        public IReadOnlyCollection<LogEntry> Entries => entries.ToArray();

        public ILogger CreateLogger(string categoryName)
        {
            return new CollectingLogger(entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger(ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);
}
