using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using HealthMate.Infrastructure.Data.DbHelper;
using System.Text;

namespace HealthMate.Application.Manager.SinaChatbot
{
    public class ContextBuilder
    {
        private readonly HealthMateContext _context;

        public ContextBuilder(HealthMateContext context)
        {
            _context = context;
        }

        public async Task<string> BuildContextAsync(string userPrompt, int patientId)
        {
            var resolvedBuilder = new StringBuilder();
            var unresolvedTags = new List<string>();

            // Match @TagType-Identifier (including dashes, slashes, and dots)
            var matches = Regex.Matches(userPrompt, @"@[\w\-/\.]+");

            foreach (Match match in matches)
            {
                var tag = match.Value;
                var resolved = await ResolveTagAsync(tag, patientId);

                if (resolved == null)
                {
                    unresolvedTags.Add(tag);
                }
                else
                {
                    resolvedBuilder.AppendLine(resolved);

                    // Replace tag in user prompt with friendly description
                    userPrompt = userPrompt.Replace(tag, GetTagLabel(tag));
                }
            }

            var intro = "You are a medical doctor assistant AI. The user is a healthcare provider asking questions about the following patient.\n";
            var unresolvedNotice = unresolvedTags.Count > 0
                ? $"Wrong Reference: {string.Join(", ", unresolvedTags)}\n\n"
                : "";

            var context = intro + unresolvedNotice + resolvedBuilder.ToString() + "\nUser Prompt:\n" + userPrompt;
            return context;
        }

        private string GetTagLabel(string tag)
        {
            if (tag.StartsWith("@Prescription-"))
                return "this prescription";
            else if (tag.StartsWith("@LabTest-"))
                return "this lab test";
            else if (tag.StartsWith("@Observation-"))
                return "this observation";

            return "this reference";
        }

        private async Task<string?> ResolveTagAsync(string tag, int patientId)
        {
            if (tag.StartsWith("@Prescription-"))
            {
                var name = tag.Substring("@Prescription-".Length);
                return await ResolvePrescription(name, patientId);
            }
            else if (tag.StartsWith("@LabTest-"))
            {
                var name = tag.Substring("@LabTest-".Length);
                return await ResolveLabTest(name, patientId);
            }
            else if (tag.StartsWith("@Observation-"))
            {
                var name = tag.Substring("@Observation-".Length);
                return await ResolveObservation(name, patientId);
            }

            return null;
        }

        private async Task<string?> ResolvePrescription(string nameIdentifier, int patientId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PatientMedicines!)
                    .ThenInclude(pm => pm.Medicine)
                .Where(p => p.PatientId == patientId &&
                            p.NameIdentifier == nameIdentifier)
                .FirstOrDefaultAsync();

            if (prescription == null) return null;

            var lines = prescription.PatientMedicines!
                .Select(pm =>
                    $"{pm.Medicine.Name}: {pm.Dosage}, every {pm.FrequencyInHours}h for {pm.DurationInDays}d");

            return $"Prescription ({prescription.NameIdentifier}) from {prescription.PrescriptionDate:yyyy-MM-dd}:\n" +
                   string.Join("\n", lines);
        }

        private async Task<string?> ResolveLabTest(string nameIdentifier, int patientId)
        {
            var labTest = await _context.LabTests
                .Include(l => l.LabTestResults!)
                    .ThenInclude(r => r.LabTestAttribute)
                .Where(l => l.patientId == patientId && l.NameIdentifier == nameIdentifier)
                .FirstOrDefaultAsync();

            if (labTest == null) return null;

            var results = labTest.LabTestResults!
                .Select(r =>
                    $"{r.LabTestAttribute.Abbreviation}: {r.Value} {r.LabTestAttribute.ValueUnit} (Normal: {r.LabTestAttribute.NormalRange})");

            return $"Lab Test '{labTest.LabTestName}' on {labTest.RecordedTime:yyyy-MM-dd}:\n" +
                string.Join("\n", results);
        }

        private async Task<string?> ResolveObservation(string nameIdentifier, int patientId)
        {
            var observation = await _context.Observations
                .Where(o => o.PatientId == patientId &&
                            o.NameIdentifier == nameIdentifier)
                .OrderByDescending(o => o.DateOfObservation)
                .FirstOrDefaultAsync();

            if (observation == null) return null;

            return $"Observation '{observation.CodeDisplayName ?? observation.Code}' " +
                $"on {observation.DateOfObservation:yyyy-MM-dd}:\n" +
                $"{observation.ValueQuanitity} {observation.ValueUnit} – " +
                $"Interpretation: {observation.Interpertation}";
        }
    }
}
