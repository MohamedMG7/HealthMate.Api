using HealthMate.Sina.Llm;
using HealthMate.Sina.Llm.Providers;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Sessions;
using HealthMate.Sina.Tools;
using HealthMate.Sina.Tools.DrugInteractions;
using HealthMate.Sina.Tools.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthMate.Sina;

public static class DependencyInjection
{
    public static IServiceCollection AddSina(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SinaLlmConfig>(configuration.GetSection(SinaLlmConfig.SectionName));
        services.AddHttpClient<GeminiAdapter>().AddStandardResilienceHandler();
        services.AddHttpClient<OpenAiAdapter>().AddStandardResilienceHandler();
        services.AddScoped<ILlmProviderSelector, LlmProviderSelector>();

        services.AddScoped<IContextSummarizer, ContextSummarizer>();
        services.AddScoped<IProactiveAlertEngine, ProactiveAlertEngine>();
        services.AddScoped<ISinaSafetyFilter, SinaSafetyFilter>();
        services.AddScoped<IDrugInteractionLookup, LocalRulesDrugInteractionLookup>();

        services.AddScoped<ISinaTool, GetPatientSummaryTool>();
        services.AddScoped<ISinaTool, GetLabTestTool>();
        services.AddScoped<ISinaTool, SearchObservationsTool>();
        services.AddScoped<ISinaTool, GetPrescriptionHistoryTool>();
        services.AddScoped<ISinaTool, GetEncounterNoteTool>();
        services.AddScoped<ISinaTool, CheckDrugInteractionsTool>();
        services.AddScoped<ISinaTool, CheckAllergyConflictTool>();
        services.AddScoped<ToolRegistry>();
        services.AddScoped<SinaManager>();

        return services;
    }
}
