using FluentAssertions;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Tools.DrugInteractions;

namespace HealthMate.Tests.Sina.Tools;

public sealed class LocalRulesDrugInteractionLookupTests
{
    [Fact]
    public void FindInteractions_returns_known_pair_case_insensitive()
    {
        var sut = new LocalRulesDrugInteractionLookup();
        var medications = new[]
        {
            Medication(1, 10, "Warfarin"),
            Medication(2, 11, "ASPIRIN")
        };

        var result = sut.FindInteractions(medications);

        result.Should().ContainSingle();
        result[0].Severity.Should().Be("severe");
    }

    [Fact]
    public void FindInteractions_returns_empty_for_missing_pair()
    {
        var sut = new LocalRulesDrugInteractionLookup();
        var medications = new[]
        {
            Medication(1, 10, "Metformin"),
            Medication(2, 11, "Paracetamol")
        };

        sut.FindInteractions(medications).Should().BeEmpty();
    }

    private static ActiveMedicationSummary Medication(int patientMedicineId, int medicineId, string name)
    {
        return new ActiveMedicationSummary(patientMedicineId, medicineId, $"#PM-{patientMedicineId}", name, name, "1 tablet", 12, 10, DateTime.UtcNow);
    }
}
