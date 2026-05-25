using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.DrugInteractions;

public interface IDrugInteractionLookup
{
    IReadOnlyList<DrugInteractionMatch> FindInteractions(IReadOnlyList<ActiveMedicationSummary> medications);
}

public record DrugInteractionMatch(
    int DrugAId,
    int DrugBId,
    string DrugAName,
    string DrugBName,
    string Severity,
    string Source,
    string Description);
