using HealthMate.Api;
using HealthMate.Application.Manager.AccountManager;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace HealthMate.Tests.Infrastructure;

public sealed class WebAppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("healthmate_tests")
        .WithUsername("healthmate")
        .WithPassword("changeme")
        .Build();

    public HttpClient Client { get; private set; } = null!;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        // These have to be environment variables (not ConfigureAppConfiguration),
        // because Program.Main reads Jwt:Key during AddSharedInfrastructure (which
        // is DI registration time, before WebApplication.Build()). The factory's
        // ConfigureAppConfiguration callbacks only apply during Build(), so any
        // value they add is invisible to the registration-time validation.
        // Env vars are picked up by WebApplication.CreateBuilder's default sources.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", "TEST_ONLY_JWT_SIGNING_KEY_NOT_SECRET_00000000000000000000000000000000");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "HealthMate_Servers");
        Environment.SetEnvironmentVariable("Jwt__Audience", "HealthMate_Clients");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:4200");
        Environment.SetEnvironmentVariable("MlService__BaseUrl", "http://ml-test.invalid");
        Environment.SetEnvironmentVariable("MlService__AuthToken", "test-only-token");

        Factory = new TestWebApplicationFactory(postgres.GetConnectionString());
        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthMateContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await postgres.DisposeAsync();
    }

    private sealed class TestWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<HealthMateContext>>();
                services.RemoveAll<IDbContextFactory<HealthMateContext>>();
                services.RemoveAll<IEmailService>();

                services.AddDbContext<HealthMateContext>(options => options.UseNpgsql(connectionString));
                services.AddDbContextFactory<HealthMateContext>(options => options.UseNpgsql(connectionString), ServiceLifetime.Scoped);
                services.AddScoped<IEmailService, NoopEmailService>();
            });
        }
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task<string> SendEmailAsync(string email, string subject, string message) => Task.FromResult("Email Sent");
    }
}
