using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Trainer.Data;

namespace Trainer.Api.Tenancy;

/// <summary>
/// Используется только инструментом dotnet ef (migrations, database update).
/// В runtime DbContext создаётся через стандартный DI и не трогает этот класс.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainerDbContext>
{
    public TrainerDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Design-time connection string is missing. " +
                "Set it via: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<conn>\"");

        var options = new DbContextOptionsBuilder<TrainerDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TrainerDbContext(options, new DesignTimeTenantContext());
    }
}
