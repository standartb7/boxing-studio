using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trainer.App.Pages;
using Trainer.App.Services;
using Trainer.App.ViewModels;
using Trainer.Core.Abstractions;
using Trainer.Data;

namespace Trainer.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "trainer.db");
		builder.Services.AddTrainerData(opt => opt.UseSqlite($"Data Source={dbPath}"));

		// Tenant context: на этапе 1a — стабильный per-device GUID, на этапе 3 заменим на JWT-реализацию.
		builder.Services.AddSingleton<ITenantContext, DeviceTenantContext>();

		builder.Services.AddTransient<ClientsViewModel>();
		builder.Services.AddTransient<ClientsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
#if DEBUG
			// Этап 1a: схема изменилась (добавилось поле TenantId). Старую локальную БД пересоздаём.
			// Когда переедем на Postgres + EF миграции, этот блок уйдёт.
			db.Database.EnsureDeleted();
#endif
			db.Database.EnsureCreated();
		}

		return app;
	}
}
