using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Core.Abstractions;
using Trainer.Data.Services;

namespace Trainer.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует DbContext и доменные сервисы.
    /// Провайдер БД выбирает вызывающая сторона: MAUI → UseSqlite, Web API → UseNpgsql.
    /// </summary>
    public static IServiceCollection AddTrainerData(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<TrainerDbContext>(configureDb);

        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IWorkoutTemplateService, WorkoutTemplateService>();

        return services;
    }
}
