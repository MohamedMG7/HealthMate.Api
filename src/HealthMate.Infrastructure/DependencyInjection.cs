using System.Text;
using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Application.Abstractions.Storage;
using HealthMate.Application.Abstractions.Validation;
using HealthMate.Application.Identity;
using HealthMate.Application.Manager;
using HealthMate.Application.Manager.AdminManager;
using HealthMate.Application.Manager.BodySiteManager;
using HealthMate.Application.Manager.ConditionManager;
using HealthMate.Application.Manager.DiseaseManager;
using HealthMate.Application.Manager.EncounterManager;
using HealthMate.Application.Manager.HealthCareProviderManager;
using HealthMate.Application.Manager.HealthRecordManager;
using HealthMate.Application.Manager.LabTestManager;
using HealthMate.Application.Manager.MachineLearningManager;
using HealthMate.Application.Manager.MedicalRecordManager;
using HealthMate.Application.Manager.MessageManager;
using HealthMate.Application.Manager.ObservationManager;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Application.Managers;
using HealthMate.Application.Patients.Services;
using HealthMate.Application.Prescriptions.Contracts.Medicines;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Fhir.Ports;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Fhir;
using HealthMate.Infrastructure.Identity.Adapters;
using HealthMate.Infrastructure.Identity.Repositories;
using HealthMate.Infrastructure.Identity.Services;
using HealthMate.Infrastructure.Persistence.Repositories;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.AdminRepos;
using HealthMate.Infrastructure.Repositories.ConditionRepos;
using HealthMate.Infrastructure.Repositories.HealthCareProviderRepos;
using HealthMate.Infrastructure.Repositories.HealthRecordRepos;
using HealthMate.Infrastructure.Repositories.Interfaces;
using HealthMate.Infrastructure.Repositories.MessageRepos;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using HealthMate.Infrastructure.Repositories.PatientAllergyRepos;
using HealthMate.Infrastructure.Sina;
using HealthMate.Infrastructure.Storage;
using HealthMate.Sina.Ports;
using HealthMate.Sina.Sessions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace HealthMate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PatientLastUpdatedInterceptor>();
        services.AddScoped<PatientHistoryWriter>();

        services.AddDbContext<HealthMateContext>((sp, options) =>
            ConfigureHealthMateContext(options, configuration, sp));

        services.AddDbContextFactory<HealthMateContext>((sp, options) =>
            ConfigureHealthMateContext(options, configuration, sp), ServiceLifetime.Scoped);

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

        services.AddScoped<IGenericRepository<Animal>, GenericRepository<Animal>>();
        services.AddScoped<IGenericRepository<BodySite>, GenericRepository<BodySite>>();
        services.AddScoped<IGenericRepository<Condition>, GenericRepository<Condition>>();
        services.AddScoped<IGenericRepository<Disease>, GenericRepository<Disease>>();
        services.AddScoped<IGenericRepository<Encounter>, GenericRepository<Encounter>>();
        services.AddScoped<IGenericRepository<LabTest>, GenericRepository<LabTest>>();
        services.AddScoped<IGenericRepository<LabTestResult>, GenericRepository<LabTestResult>>();
        services.AddScoped<IGenericRepository<MedicalImage>, GenericRepository<MedicalImage>>();
        services.AddScoped<IGenericRepository<Observation>, GenericRepository<Observation>>();
        services.AddScoped<IGenericRepository<Prescription>, GenericRepository<Prescription>>();
        services.AddScoped<IEncounterRepository, EfEncounterRepository>();
        services.AddScoped<IPatientRepository, EfPatientRepository>();
        services.AddScoped<IPatientAllergyRepo, PatientAllergyRepo>();
        services.AddScoped<IObservationRepo, ObservationRepo>();
        services.AddScoped<IConditionRepo, ConditionRepo>();
        services.AddScoped<IValidationRepo, ValidationRepo>();
        services.AddScoped<ILabTestAttributeRepo, LabTestAttributeRepo>();
        services.AddScoped<IMedicineRepo, MedicineRepo>();
        services.AddScoped<IHealthRecordRepo, HealthRecordRepo>();
        services.AddScoped<IMessageRepo, MessageRepo>();
        services.AddScoped<IMentalHealthAssessmentRepo, MentalHealthAssessmentRepository>();
        services.AddScoped<IHealthCareProviderRepo, HealthCareProviderRepo>();
        services.AddScoped<IAdminRepo, AdminRepo>();
        services.AddScoped<IReporterRepo, ReporterRepo>();

        services.AddScoped<IVerificationCodeRepo, VerificationCodeRepo>();
        services.AddScoped<IVerificationCodeStore, VerificationCodeStore>();
        services.AddScoped<IApplicationUserRepo, ApplicationUserRepo>();
        services.AddScoped<IIdentityUserDirectory, IdentityUserDirectory>();
        services.AddScoped<IIdentityAccountGateway, AspNetIdentityAccountGateway>();
        services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IUserLinkedAccountLookup, EfUserLinkedAccountLookup>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.AddScoped<IPatientAccountReader, EfPatientAccountReader>();
        services.AddScoped<IFhirPatientStore, InProcessFhirPatientStore>();
        services.AddScoped<ISinaClock, SystemSinaClock>();
        services.AddScoped<ISinaClinicalReader, InProcessSinaClinicalReader>();
        services.AddScoped<ISinaSessionStore, SinaSessionStore>();

        services.AddScoped<IPatientManager, PatientManager>();
        services.AddScoped<IConditionManager, ConditionManager>();
        services.AddScoped<IEncounterManager, EncounterManager>();
        services.AddScoped<IObservationManager, ObservationManager>();
        services.AddScoped<IBodySiteManager, BodySiteManager>();
        services.AddScoped<IDiseaseManager, DiseaseManager>();
        services.AddScoped<ILabTestManager, LabTestManager>();
        services.AddScoped<IHealthRecordManager, HealthRecordManager>();
        services.AddScoped<IRecordImageManager, RecordImageManager>();
        services.AddScoped<IMessageManager, MessageManager>();
        services.AddScoped<IMentalHealthAssessmentManager, MentalHealthAssessmentManager>();
        services.AddScoped<IHealthCareProviderManager, HealthCareProviderManager>();
        services.AddScoped<IAdminManager, AdminManager>();
        services.AddScoped<IReporter, Reporter>();
        services.AddScoped<IMachineLearningManager, MachineLearningManager>();

        return services;
    }

    private static void ConfigureHealthMateContext(
        DbContextOptionsBuilder options,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        var interceptors = new List<IInterceptor>();
        if (serviceProvider.GetService<PatientLastUpdatedInterceptor>() is { } lastUpdated)
        {
            interceptors.Add(lastUpdated);
        }

        if (serviceProvider.GetService<PatientHistoryWriter>() is { } historyWriter)
        {
            interceptors.Add(historyWriter);
        }

        if (interceptors.Count > 0)
        {
            options.AddInterceptors(interceptors);
        }
    }
}
