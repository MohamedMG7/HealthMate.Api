//
//This is just a wrapper now, but it keeps the architecture clean and extensible. 
//Later, you can add tag preprocessing or transformation logic here.
//
namespace HealthMate.Application.Manager.SinaChatbot
{
    public class PromptResolver
    {
        private readonly ContextBuilder _contextBuilder;

        public PromptResolver(ContextBuilder contextBuilder)
        {
            _contextBuilder = contextBuilder;
        }

        public async Task<string> ResolvePromptAsync(string prompt, int patientId)
        {
            return await _contextBuilder.BuildContextAsync(prompt, patientId);
        }
    }
}
