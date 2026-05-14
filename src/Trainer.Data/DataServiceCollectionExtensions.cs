using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Core.Abstractions;
using Trainer.Data.Services;

namespace Trainer.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddTrainerData(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<TrainerDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ITrainingTypeService, TrainingTypeService>();
        services.AddScoped<ISessionService, SessionService>();

        return services;
    }
}
