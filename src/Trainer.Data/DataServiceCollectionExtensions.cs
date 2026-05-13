using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Core.Abstractions;
using Trainer.Data.Repositories;
using Trainer.Data.Services;

namespace Trainer.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddTrainerData(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<TrainerDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IWorkoutTemplateService, WorkoutTemplateService>();

        return services;
    }
}
