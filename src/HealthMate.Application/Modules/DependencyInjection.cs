using System.Text;
using HealthMate.Application.Manager;
using HealthMate.Application.Manager.AccountManager;
using HealthMate.Application.Manager.AdminManager;
using HealthMate.Application.Manager.BodySiteManager;
using HealthMate.Application.Manager.ConditionManager;
using HealthMate.Application.Manager.DiseaseManager;
using HealthMate.Application.Manager.DocumentManager;
using HealthMate.Application.Manager.EncounterManager;
using HealthMate.Application.Manager.HealthCareProviderManager;
using HealthMate.Application.Manager.HealthRecordManager;
using HealthMate.Application.Manager.LabTestManager;
using HealthMate.Application.Manager.MachineLearningManager;
using HealthMate.Application.Manager.MedicalRecordManager;
using HealthMate.Application.Manager.MessageManager;
using HealthMate.Application.Manager.ObservationManager;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Application.Manager.SinaChatbot;
using HealthMate.Application.Manager.UsersManager;
using HealthMate.Application.Manager.UtilityManager;
using HealthMate.Application.Managers;
using HealthMate.Application.Services;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.AdminRepos;
using HealthMate.Infrastructure.Repositories.ApplicationUserRepos;
using HealthMate.Infrastructure.Repositories.ConditionRepos;
using HealthMate.Infrastructure.Repositories.HealthCareProviderRepos;
using HealthMate.Infrastructure.Repositories.HealthRecordRepos;
using HealthMate.Infrastructure.Repositories.Interfaces;
using HealthMate.Infrastructure.Repositories.MessageRepos;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using HealthMate.Infrastructure.Repositories.PatientRepos;
using HealthMate.Infrastructure.Repositories.VerificationCodeRepo;
using HealthMate.Infrastructure.Repositories.VerificationCodeRepos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HealthMate.Application.Modules;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("HealthmateWebsite", policy =>
            {
                policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddDbContext<HealthMateContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddDbContextFactory<HealthMateContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<HealthMateContext>().AddDefaultTokenProviders();

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Jwt:Key is not configured.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("PatientOnly", policy => policy.RequireRole("Patient"));
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("HealthCareProviderOnly", policy => policy.RequireRole("HealthCareProvider"));
            options.AddPolicy("PatientOrHealthCareProvider", policy => policy.RequireRole("Patient", "HealthCareProvider"));
        });

        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IUtilityManager, UtilityManager>();

        return services;
    }

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IGenericRepository<VerificationCode>, GenericRepository<VerificationCode>>();
        services.AddScoped<IVerificationCodeRepo, VerificationCodeRepo>();
        services.AddScoped<IApplicationUserRepo, ApplicationUserRepo>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IAccountManager, AccountManager>();
        return services;
    }

    public static IServiceCollection AddPatientsModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<Animal>, GenericRepository<Animal>>();
        services.AddScoped<IGenericRepository<Patient>, GenericRepository<Patient>>();
        services.AddScoped<IPatientRepo, PatientRepo>();
        services.AddScoped<IPatientManager, PatientManager>();
        return services;
    }

    public static IServiceCollection AddClinicalModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<Condition>, GenericRepository<Condition>>();
        services.AddScoped<IGenericRepository<Encounter>, GenericRepository<Encounter>>();
        services.AddScoped<IGenericRepository<Observation>, GenericRepository<Observation>>();
        services.AddScoped<IGenericRepository<BodySite>, GenericRepository<BodySite>>();
        services.AddScoped<IGenericRepository<Disease>, GenericRepository<Disease>>();
        services.AddScoped<IObservationRepo, ObservationRepo>();
        services.AddScoped<IConditionRepo, ConditionRepo>();
        services.AddScoped<IValidationRepo, ValidationRepo>();
        services.AddScoped<IConditionManager, ConditionManager>();
        services.AddScoped<IEncounterManager, EncounterManager>();
        services.AddScoped<IObservationManager, ObservationManager>();
        services.AddScoped<IBodySiteManager, BodySiteManager>();
        services.AddScoped<IDiseaseManager, DiseaseManager>();
        return services;
    }

    public static IServiceCollection AddLabTestsModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<LabTest>, GenericRepository<LabTest>>();
        services.AddScoped<IGenericRepository<LabTestResult>, GenericRepository<LabTestResult>>();
        services.AddScoped<ILabTestAttributeRepo, LabTestAttributeRepo>();
        services.AddScoped<ILabTestManager, LabTestManager>();
        return services;
    }

    public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<Prescription>, GenericRepository<Prescription>>();
        services.AddScoped<IGenericRepository<Medicine>, GenericRepository<Medicine>>();
        services.AddScoped<IGenericRepository<PatientMedicine>, GenericRepository<PatientMedicine>>();
        services.AddScoped<IMedicineRepo, MedicineRepo>();
        return services;
    }

    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<MedicalImage>, GenericRepository<MedicalImage>>();
        services.AddScoped<IHealthRecordRepo, HealthRecordRepo>();
        services.AddScoped<IHealthRecordManager, HealthRecordManager>();
        services.AddScoped<IRecordImageManager, RecordImageManager>();
        services.AddScoped<IDocumentManager, DocumentManager>();
        return services;
    }

    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        services.AddScoped<IMessageRepo, MessageRepo>();
        services.AddScoped<IMessageManager, MessageManager>();
        return services;
    }

    public static IServiceCollection AddMentalHealthModule(this IServiceCollection services)
    {
        services.AddScoped<IMentalHealthAssessmentRepo, MentalHealthAssessmentRepository>();
        services.AddScoped<IMentalHealthAssessmentManager, MentalHealthAssessmentManager>();
        return services;
    }

    public static IServiceCollection AddProvidersModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<HealthCareProvider>, GenericRepository<HealthCareProvider>>();
        services.AddScoped<IHealthCareProviderRepo, HealthCareProviderRepo>();
        services.AddScoped<IHealthCareProviderManager, HealthCareProviderManager>();
        return services;
    }

    public static IServiceCollection AddAdminModule(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<Admin>, GenericRepository<Admin>>();
        services.AddScoped<IAdminRepo, AdminRepo>();
        services.AddScoped<IReporterRepo, ReporterRepo>();
        services.AddScoped<IAdminManager, AdminManager>();
        services.AddScoped<IReporter, Reporter>();
        return services;
    }

    public static IServiceCollection AddSinaModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ContextBuilder>();
        services.AddScoped<PromptResolver>();
        services.AddScoped<SinaManager>();
        services.AddHttpClient<GeminiClient>();
        services.Configure<GeminiConfig>(configuration.GetSection("Gemini"));
        return services;
    }

    public static IServiceCollection AddMlModule(this IServiceCollection services, IConfiguration configuration)
    {
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

        services.AddScoped<IMachineLearningManager, MachineLearningManager>();
        return services;
    }
}
