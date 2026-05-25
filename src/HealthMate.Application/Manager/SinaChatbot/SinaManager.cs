using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Application.Manager.SinaChatbot
{
    public class SinaManager
    {
        private readonly PromptResolver _promptResolver;
        private readonly GeminiClient _geminiClient;
        private readonly HealthMateContext _context;

        public SinaManager(PromptResolver promptResolver, GeminiClient geminiClient,HealthMateContext context)
        {
            _promptResolver = promptResolver;
            _geminiClient = geminiClient;
            _context = context;
        }

        public async Task<string> HandlePromptAsync(string rawPrompt, int patientId)
        {
            var fullPrompt = await _promptResolver.ResolvePromptAsync(rawPrompt, patientId);
            return await _geminiClient.AskRawPromptAsync(fullPrompt);
        }

        public async Task<object> GetMCPReferencesAsync(int patientId)
        {
            var prescriptions = await _context.Prescriptions
                .Where(p => p.PatientId == patientId && !string.IsNullOrEmpty(p.NameIdentifier))
                .Select(p => p.NameIdentifier).AsNoTracking()
                .ToListAsync();

            var observations = await _context.Observations
                .Where(o => o.PatientId == patientId && !string.IsNullOrEmpty(o.NameIdentifier))
                .Select(o => o.NameIdentifier).AsNoTracking()
                .ToListAsync();

            var labTests = await _context.LabTests
                .Where(l => l.patientId == patientId && !string.IsNullOrEmpty(l.NameIdentifier))
                .Select(l => l.NameIdentifier).AsNoTracking()
                .ToListAsync();

            return new
            {
                prescriptions,
                observations,
                labTests
            };
        }
    }
}
