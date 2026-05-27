using System.Reflection;
using FluentValidation;
using HealthMate.Application.Common;
using HealthMate.Application.Common.Behaviors;
using HealthMate.Application.Common.Time;
using HealthMate.Application.Identity;
using HealthMate.Application.Manager.DocumentManager;
using HealthMate.Application.Manager.MachineLearningManager;
using HealthMate.Application.Manager.UsersManager;
using HealthMate.Application.Manager.UtilityManager;
using HealthMate.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HealthMate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IHandlerDispatcher, HandlerDispatcher>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddScoped<IUtilityManager, UtilityManager>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IAccountManager, AccountManager>();
        services.AddScoped<IDocumentManager, DocumentManager>();

        services.Configure<MlServiceOptions>(configuration.GetSection(MlServiceOptions.SectionName));
        services.AddHttpClient<IMlGateway, MlGateway>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<MlServiceOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.BaseUrl))
            {
                throw new InvalidOperationException("MlService:BaseUrl is not configured.");
            }

            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(5);
            if (!string.IsNullOrWhiteSpace(opts.AuthToken))
            {
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.AuthToken);
            }
        })
        .AddStandardResilienceHandler();

        RegisterHandlersAndValidators(services, typeof(DependencyInjection).Assembly);
        return services;
    }

    private static void RegisterHandlersAndValidators(IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(static type => type is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var serviceType in type.GetInterfaces())
            {
                if (!serviceType.IsGenericType)
                {
                    continue;
                }

                var genericDefinition = serviceType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(IHandler<,>))
                {
                    services.AddScoped(serviceType, type);
                }
                else if (genericDefinition == typeof(IValidator<>))
                {
                    services.AddScoped(serviceType, type);
                }
            }
        }
    }
}
