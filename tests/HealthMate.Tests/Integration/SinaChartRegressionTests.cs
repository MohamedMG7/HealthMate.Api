using System.Net;
using FluentAssertions;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Sina.Ports;
using HealthMate.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HealthMate.Tests.Integration;

public sealed class SinaChartRegressionTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task Chart_summary_after_full_doctor_flow_returns_all_aggregates()
    {
        var (patientId, providerId) = await EncounterIntegrationTestHelpers.SeedPatientAndHcpAsync(
            fixture.Factory.Services, "sina-chart-full");

        var diseaseName = "Regression_Disease_" + Guid.NewGuid().ToString("N")[..8];
        var firstMedicineName = "Regression_Med_A_" + Guid.NewGuid().ToString("N")[..8];
        var secondMedicineName = "Regression_Med_B_" + Guid.NewGuid().ToString("N")[..8];
        int diseaseId, firstMedicineId, secondMedicineId;

        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<HealthMateContext>();
            diseaseId = await EncounterIntegrationTestHelpers.SeedDiseaseAsync(ctx, diseaseName);
            firstMedicineId = await EncounterIntegrationTestHelpers.SeedMedicineAsync(ctx, firstMedicineName);
            secondMedicineId = await EncounterIntegrationTestHelpers.SeedMedicineAsync(ctx, secondMedicineName);
        }

        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        const string reasonToVisit = "Regression visit reason";
        var startRes = await client.PostAsync("/api/Encounter/start",
            EncounterIntegrationTestHelpers.JsonContent(new { patientId, healthCareProviderId = providerId, reasonToVisit }));
        startRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var startBody = await EncounterIntegrationTestHelpers.ReadJsonAsync<StartEncounterResult>(startRes);
        var encounterId = startBody.EncounterId;

        var obs1 = await client.PostAsync($"/api/Encounter/{encounterId}/observations",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                category = ObservationCategory.VitalSigns,
                code = "hr",
                codeDisplayName = "Heart Rate",
                valueQuantity = 72m,
                valueUnit = "bpm",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = DateTime.UtcNow.AddMinutes(-30),
                nameIdentifier = (string?)null
            }));
        obs1.StatusCode.Should().Be(HttpStatusCode.Created);

        var obs2 = await client.PostAsync($"/api/Encounter/{encounterId}/observations",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                category = ObservationCategory.VitalSigns,
                code = "bp",
                codeDisplayName = "Blood Pressure",
                valueQuantity = 120m,
                valueUnit = "mmHg",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = DateTime.UtcNow.AddMinutes(-25),
                nameIdentifier = (string?)null
            }));
        obs2.StatusCode.Should().Be(HttpStatusCode.Created);

        var condRes = await client.PostAsync($"/api/Encounter/{encounterId}/conditions",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                diseaseId,
                severity = Severity.Moderate,
                clinicalStatus = ClinicalStatus.Active,
                dateRecorded = DateTime.UtcNow.AddMinutes(-20),
                note = (string?)null
            }));
        condRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var rxRes = await client.PostAsync($"/api/Encounter/{encounterId}/prescription",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId = firstMedicineId, dosage = "10mg", frequencyInHours = 8, durationInDays = 30 },
                    new { medicineId = secondMedicineId, dosage = "20mg", frequencyInHours = 12, durationInDays = 30 }
                }
            }));
        rxRes.StatusCode.Should().Be(HttpStatusCode.Created);

        const string treatmentPlan = "Regression treatment plan";
        const string note = "Regression note";
        var endRes = await client.PostAsync($"/api/Encounter/{encounterId}/end",
            EncounterIntegrationTestHelpers.JsonContent(new { treatmentPlan, note }));
        endRes.StatusCode.Should().Be(HttpStatusCode.OK);

        using var readerScope = fixture.Factory.Services.CreateScope();
        var reader = readerScope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var chart = await reader.GetPatientChartAsync(patientId, CancellationToken.None);

        chart.Should().NotBeNull();
        chart!.PatientId.Should().Be(patientId);

        chart.ActiveConditions.Should().HaveCount(1);
        chart.ActiveConditions[0].Name.Should().Be(diseaseName);
        chart.ActiveConditions[0].Severity.Should().Be(Severity.Moderate.ToString());

        chart.RecentEncounters.Should().HaveCount(1);
        chart.RecentEncounters[0].Id.Should().Be(encounterId);
        chart.RecentEncounters[0].Reason.Should().Be(reasonToVisit);
        chart.RecentEncounters[0].TreatmentPlan.Should().Be(treatmentPlan);
        chart.RecentEncounters[0].Note.Should().Be(note);
        chart.RecentEncounters[0].End.Should().BeAfter(chart.RecentEncounters[0].Start);

        chart.CurrentMedications.Should().HaveCount(2);
        chart.CurrentMedications.Should().OnlyContain(m => m.DurationInDays == 30);

        chart.Allergies.Should().BeEmpty();
        chart.RecentAbnormalLabs.Should().BeEmpty();
    }

    [Fact]
    public async Task Chart_summary_for_patient_with_no_data_is_empty_but_not_null()
    {
        var (patientId, _) = await EncounterIntegrationTestHelpers.SeedPatientAndHcpAsync(
            fixture.Factory.Services, "sina-chart-empty");

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var chart = await reader.GetPatientChartAsync(patientId, CancellationToken.None);

        chart.Should().NotBeNull();
        chart!.PatientId.Should().Be(patientId);
        chart.ActiveConditions.Should().BeEmpty();
        chart.Allergies.Should().BeEmpty();
        chart.CurrentMedications.Should().BeEmpty();
        chart.RecentEncounters.Should().BeEmpty();
        chart.RecentAbnormalLabs.Should().BeEmpty();
    }

    [Fact]
    public async Task Chart_summary_returns_null_for_unknown_patient()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var chart = await reader.GetPatientChartAsync(999999, CancellationToken.None);

        chart.Should().BeNull();
    }

    [Fact]
    public async Task Encounter_note_lookup_returns_data_for_ended_encounter()
    {
        var (patientId, providerId) = await EncounterIntegrationTestHelpers.SeedPatientAndHcpAsync(
            fixture.Factory.Services, "sina-encounter-note");

        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        const string reasonToVisit = "Note lookup visit";
        const string treatmentPlan = "Note lookup treatment plan";
        const string note = "Note lookup note";

        var startRes = await client.PostAsync("/api/Encounter/start",
            EncounterIntegrationTestHelpers.JsonContent(new { patientId, healthCareProviderId = providerId, reasonToVisit }));
        startRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var startBody = await EncounterIntegrationTestHelpers.ReadJsonAsync<StartEncounterResult>(startRes);
        var encounterId = startBody.EncounterId;

        var endRes = await client.PostAsync($"/api/Encounter/{encounterId}/end",
            EncounterIntegrationTestHelpers.JsonContent(new { treatmentPlan, note }));
        endRes.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var summary = await reader.GetEncounterNoteAsync(patientId, encounterId, CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.Id.Should().Be(encounterId);
        summary.Reason.Should().Be(reasonToVisit);
        summary.TreatmentPlan.Should().Be(treatmentPlan);
        summary.Note.Should().Be(note);
        summary.End.Should().BeAfter(summary.Start);
    }

    [Fact]
    public async Task Prescription_history_includes_medicines_with_names()
    {
        var (patientId, providerId) = await EncounterIntegrationTestHelpers.SeedPatientAndHcpAsync(
            fixture.Factory.Services, "sina-rx-history");

        var medicineName = "Rx_History_Med_" + Guid.NewGuid().ToString("N")[..8];
        int medicineId;
        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<HealthMateContext>();
            medicineId = await EncounterIntegrationTestHelpers.SeedMedicineAsync(ctx, medicineName);
        }

        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var startRes = await client.PostAsync("/api/Encounter/start",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                patientId, healthCareProviderId = providerId, reasonToVisit = "Rx history visit"
            }));
        startRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var startBody = await EncounterIntegrationTestHelpers.ReadJsonAsync<StartEncounterResult>(startRes);

        var rxRes = await client.PostAsync($"/api/Encounter/{startBody.EncounterId}/prescription",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId, dosage = "5mg", frequencyInHours = 24, durationInDays = 14 }
                }
            }));
        rxRes.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var history = await reader.GetPrescriptionHistoryAsync(patientId, null, CancellationToken.None);

        history.Should().HaveCount(1);
        history[0].Medicines.Should().HaveCount(1);
        history[0].Medicines[0].MedicineName.Should().Be(medicineName);
    }

    [Fact]
    public async Task Active_medications_sees_prescription_bound_rows()
    {
        var (patientId, providerId) = await EncounterIntegrationTestHelpers.SeedPatientAndHcpAsync(
            fixture.Factory.Services, "sina-active-meds");

        var medicineName = "Active_Med_" + Guid.NewGuid().ToString("N")[..8];
        int medicineId;
        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<HealthMateContext>();
            medicineId = await EncounterIntegrationTestHelpers.SeedMedicineAsync(ctx, medicineName);
        }

        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var startRes = await client.PostAsync("/api/Encounter/start",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                patientId, healthCareProviderId = providerId, reasonToVisit = "Active meds visit"
            }));
        startRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var startBody = await EncounterIntegrationTestHelpers.ReadJsonAsync<StartEncounterResult>(startRes);

        var rxRes = await client.PostAsync($"/api/Encounter/{startBody.EncounterId}/prescription",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                publisher = "Provider_Zero",
                medicines = new[]
                {
                    new { medicineId, dosage = "10mg", frequencyInHours = 8, durationInDays = 30 }
                }
            }));
        rxRes.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var active = await reader.GetActiveMedicationsAsync(patientId, CancellationToken.None);

        active.Should().Contain(m =>
            m.MedicineId == medicineId &&
            m.MedicineName == medicineName &&
            m.Dosage == "10mg" &&
            m.DurationInDays == 30);
    }

    [Fact]
    public async Task Search_observations_filters_by_code_and_date_range()
    {
        var (patientId, providerId) = await EncounterIntegrationTestHelpers.SeedPatientAndHcpAsync(
            fixture.Factory.Services, "sina-search-obs");

        using var client = fixture.Factory.CreateClient();
        EncounterIntegrationTestHelpers.AuthorizeAsProvider(client, providerId);

        var startRes = await client.PostAsync("/api/Encounter/start",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                patientId, healthCareProviderId = providerId, reasonToVisit = "Search obs visit"
            }));
        startRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var startBody = await EncounterIntegrationTestHelpers.ReadJsonAsync<StartEncounterResult>(startRes);
        var encounterId = startBody.EncounterId;

        var obs1Res = await client.PostAsync($"/api/Encounter/{encounterId}/observations",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                category = ObservationCategory.VitalSigns,
                code = "sina-reg-hr",
                codeDisplayName = "Heart Rate",
                valueQuantity = 75m,
                valueUnit = "bpm",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = DateTime.UtcNow.AddHours(-2),
                nameIdentifier = (string?)null
            }));
        obs1Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var obs1Body = await EncounterIntegrationTestHelpers.ReadJsonAsync<RecordObservationResult>(obs1Res);

        var obs2Res = await client.PostAsync($"/api/Encounter/{encounterId}/observations",
            EncounterIntegrationTestHelpers.JsonContent(new
            {
                category = ObservationCategory.VitalSigns,
                code = "sina-reg-bp",
                codeDisplayName = "Blood Pressure",
                valueQuantity = 125m,
                valueUnit = "mmHg",
                interpretation = "normal",
                bodySiteId = (int?)null,
                dateOfObservation = DateTime.UtcNow.AddHours(-1),
                nameIdentifier = (string?)null
            }));
        obs2Res.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = fixture.Factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ISinaClinicalReader>();
        var results = await reader.SearchObservationsAsync(
            patientId, "sina-reg-hr", null, null, 10, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Id.Should().Be(obs1Body.ObservationId);
        results[0].Code.Should().Be("sina-reg-hr");
        results[0].Value.Should().Be(75m);
        results[0].Interpretation.Should().Be("normal");
    }

    private sealed record StartEncounterResult(int EncounterId, string EncounterFhirId);
    private sealed record RecordObservationResult(int ObservationId, string ObservationFhirId, int PatientId);
}
