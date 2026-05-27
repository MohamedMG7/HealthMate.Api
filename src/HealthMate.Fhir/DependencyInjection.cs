using HealthMate.Fhir.Mapping;
using HealthMate.Fhir.Search;
using HealthMate.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthMate.Fhir;

public static class DependencyInjection
{
    public static IServiceCollection AddFhir(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<PatientResourceMapper>();
        services.AddSingleton<OperationOutcomeFactory>();
        services.AddSingleton<CapabilityStatementFactory>();
        services.AddSingleton<PatientBundleAssembler>();
        services.AddSingleton<PatientSearchParser>();
        services.AddSingleton<FhirJsonService>();
        services.AddScoped<FhirContentNegotiationFilter>();
        services.AddScoped<FhirExceptionFilter>();

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<FhirContentNegotiationFilter>();
            options.Filters.AddService<FhirExceptionFilter>();
            if (!options.OutputFormatters.OfType<FhirJsonOutputFormatter>().Any())
            {
                options.OutputFormatters.Insert(0, new FhirJsonOutputFormatter());
            }
        });

        return services;
    }
}
