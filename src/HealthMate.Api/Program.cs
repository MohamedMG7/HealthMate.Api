using HealthMate.Api.Middleware;
using HealthMate.Application.Modules;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services
                .AddSharedInfrastructure(builder.Configuration)
                .AddIdentityModule(builder.Configuration)
                .AddPatientsModule()
                .AddClinicalModule()
                .AddLabTestsModule()
                .AddPrescriptionsModule()
                .AddDocumentsModule()
                .AddMessagingModule()
                .AddMentalHealthModule()
                .AddProvidersModule()
                .AddAdminModule()
                .AddSinaModule(builder.Configuration)
                .AddFhirModule(builder.Configuration)
                .AddMlModule(builder.Configuration);

            var app = builder.Build();

            if (!app.Environment.IsProduction())
            {
                using var scope = app.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
                await ctx.Database.MigrateAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("HealthmateWebsite");
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
            app.MapControllers();

            app.Run();
        }
    }
}
