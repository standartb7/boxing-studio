using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Trainer.Data;

namespace Trainer.Api.Data;

/// <summary>
/// `dotnet ef migrations` invokes this at design time. We don't load appsettings here —
/// just point at a placeholder Postgres connection; EF only needs it to know the provider.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainerDbContext>
{
    public TrainerDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<TrainerDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_stub;Username=postgres",
                npg => npg.MigrationsAssembly("Trainer.Api"))
            .Options;
        return new TrainerDbContext(opts);
    }
}
