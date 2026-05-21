using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Infrastructure;

namespace Trainer.Api.Tests;

/// <summary>
/// In-process API host backed by an in-memory SQLite database. One factory ==
/// one database instance (shared connection keeps it alive across requests).
///
/// SQLite uses NOCASE collation instead of citext, but the auth + role logic
/// is provider-independent, so this is enough coverage for B6. Postgres-specific
/// behaviour (citext) should be exercised against a real DB on CI later.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestApiFactory()
    {
        // Set BEFORE the host builds — Program.cs reads Configuration during builder setup,
        // which runs before WebApplicationFactory's ConfigureAppConfiguration hooks fire.
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", "ignored-in-tests");
        Environment.SetEnvironmentVariable("Auth__JwtIssuer", "trainer-api-test");
        Environment.SetEnvironmentVariable("Auth__JwtAudience", "trainer-app-test");
        Environment.SetEnvironmentVariable("Auth__JwtSigningKey",
            Convert.ToBase64String(new byte[32]));
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Strip every EF-related DI entry from the production registration so the
            // SQLite provider isn't poisoned by the leftover Npgsql internal services.
            var efDescriptors = services
                .Where(d => d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true
                         || d.ServiceType.FullName?.StartsWith("Npgsql", StringComparison.Ordinal) == true)
                .ToList();
            foreach (var d in efDescriptors) services.Remove(d);

            services.AddDbContext<TrainerDbContext>(opt => opt.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
