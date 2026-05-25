using HealthMate.Sina.Ports;

namespace HealthMate.Sina.Tools.DrugInteractions;

public class LocalRulesDrugInteractionLookup : IDrugInteractionLookup
{
    // Sources: FDA labels, NHS medicine safety pages, and MedlinePlus interaction guidance.
    private static readonly IReadOnlyList<DrugInteractionRule> Rules = new[]
    {
        new DrugInteractionRule("warfarin", "aspirin", "severe", "FDA/NHS", "Increased bleeding risk."),
        new DrugInteractionRule("warfarin", "ibuprofen", "severe", "FDA/NHS", "Increased bleeding risk."),
        new DrugInteractionRule("warfarin", "naproxen", "severe", "FDA/NHS", "Increased bleeding risk."),
        new DrugInteractionRule("warfarin", "amiodarone", "severe", "FDA label", "Amiodarone can increase warfarin exposure and bleeding risk."),
        new DrugInteractionRule("warfarin", "metronidazole", "severe", "FDA label", "Metronidazole can increase anticoagulant effect."),
        new DrugInteractionRule("warfarin", "trimethoprim", "severe", "FDA label", "Sulfonamide antibiotics can increase anticoagulant effect."),
        new DrugInteractionRule("warfarin", "sulfamethoxazole", "severe", "FDA label", "Sulfonamide antibiotics can increase anticoagulant effect."),
        new DrugInteractionRule("warfarin", "fluconazole", "severe", "FDA label", "Azole antifungals can increase anticoagulant effect."),
        new DrugInteractionRule("warfarin", "ciprofloxacin", "high", "FDA label", "Fluoroquinolones can increase anticoagulant effect."),
        new DrugInteractionRule("warfarin", "clarithromycin", "high", "FDA label", "Macrolides can increase anticoagulant effect."),
        new DrugInteractionRule("lisinopril", "potassium", "severe", "FDA label", "ACE inhibitors with potassium can cause hyperkalemia."),
        new DrugInteractionRule("enalapril", "potassium", "severe", "FDA label", "ACE inhibitors with potassium can cause hyperkalemia."),
        new DrugInteractionRule("losartan", "potassium", "high", "FDA label", "ARBs with potassium can cause hyperkalemia."),
        new DrugInteractionRule("spironolactone", "potassium", "severe", "FDA label", "Potassium-sparing diuretics with potassium can cause hyperkalemia."),
        new DrugInteractionRule("spironolactone", "lisinopril", "severe", "FDA label", "Combined RAAS blockade can cause hyperkalemia."),
        new DrugInteractionRule("spironolactone", "enalapril", "severe", "FDA label", "Combined RAAS blockade can cause hyperkalemia."),
        new DrugInteractionRule("spironolactone", "losartan", "severe", "FDA label", "Combined RAAS blockade can cause hyperkalemia."),
        new DrugInteractionRule("digoxin", "amiodarone", "severe", "FDA label", "Amiodarone can increase digoxin concentration."),
        new DrugInteractionRule("digoxin", "clarithromycin", "high", "FDA label", "Macrolides can increase digoxin exposure."),
        new DrugInteractionRule("simvastatin", "clarithromycin", "severe", "FDA label", "CYP3A4 inhibition increases statin myopathy risk."),
        new DrugInteractionRule("simvastatin", "erythromycin", "severe", "FDA label", "CYP3A4 inhibition increases statin myopathy risk."),
        new DrugInteractionRule("simvastatin", "ketoconazole", "severe", "FDA label", "Azole inhibition increases statin myopathy risk."),
        new DrugInteractionRule("atorvastatin", "clarithromycin", "high", "FDA label", "CYP3A4 inhibition increases statin myopathy risk."),
        new DrugInteractionRule("sildenafil", "nitroglycerin", "severe", "FDA label", "Nitrates with PDE5 inhibitors can cause severe hypotension."),
        new DrugInteractionRule("tadalafil", "nitroglycerin", "severe", "FDA label", "Nitrates with PDE5 inhibitors can cause severe hypotension."),
        new DrugInteractionRule("metformin", "iodinated contrast", "high", "FDA label", "Contrast-associated renal injury can increase lactic acidosis risk."),
        new DrugInteractionRule("lithium", "ibuprofen", "high", "FDA label", "NSAIDs can increase lithium levels."),
        new DrugInteractionRule("lithium", "naproxen", "high", "FDA label", "NSAIDs can increase lithium levels."),
        new DrugInteractionRule("lithium", "lisinopril", "high", "FDA label", "ACE inhibitors can increase lithium levels."),
        new DrugInteractionRule("methotrexate", "trimethoprim", "severe", "FDA label", "Additive antifolate toxicity and marrow suppression risk."),
        new DrugInteractionRule("methotrexate", "sulfamethoxazole", "severe", "FDA label", "Additive antifolate toxicity and marrow suppression risk."),
        new DrugInteractionRule("methotrexate", "ibuprofen", "high", "FDA label", "NSAIDs can increase methotrexate toxicity."),
        new DrugInteractionRule("theophylline", "ciprofloxacin", "high", "FDA label", "Ciprofloxacin can increase theophylline concentration."),
        new DrugInteractionRule("phenytoin", "fluconazole", "high", "FDA label", "Fluconazole can increase phenytoin concentration."),
        new DrugInteractionRule("carbamazepine", "clarithromycin", "high", "FDA label", "Clarithromycin can increase carbamazepine concentration."),
        new DrugInteractionRule("valproate", "lamotrigine", "high", "FDA label", "Valproate increases lamotrigine exposure and rash risk."),
    };

    public IReadOnlyList<DrugInteractionMatch> FindInteractions(IReadOnlyList<ActiveMedicationSummary> medications)
    {
        var matches = new List<DrugInteractionMatch>();
        for (var i = 0; i < medications.Count; i++)
        {
            for (var j = i + 1; j < medications.Count; j++)
            {
                var first = medications[i];
                var second = medications[j];
                var rule = Rules.FirstOrDefault(r => r.Matches(first, second));
                if (rule is null)
                {
                    continue;
                }

                matches.Add(new DrugInteractionMatch(
                    first.PatientMedicineId,
                    second.PatientMedicineId,
                    first.MedicineName,
                    second.MedicineName,
                    rule.Severity,
                    rule.Source,
                    rule.Description));
            }
        }

        return matches;
    }

    private sealed record DrugInteractionRule(string DrugA, string DrugB, string Severity, string Source, string Description)
    {
        public bool Matches(ActiveMedicationSummary first, ActiveMedicationSummary second)
        {
            return ContainsDrug(first, DrugA) && ContainsDrug(second, DrugB)
                || ContainsDrug(first, DrugB) && ContainsDrug(second, DrugA);
        }

        private static bool ContainsDrug(ActiveMedicationSummary medication, string needle)
        {
            return Contains(medication.MedicineName, needle) || Contains(medication.ActiveIngredients, needle);
        }

        private static bool Contains(string? haystack, string needle)
        {
            return haystack?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
