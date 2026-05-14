using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trainer.App.Pages;
using Trainer.App.Services;
using Trainer.App.ViewModels;
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

		// --- Локальная БД (SQLite) ---
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "trainer.db");
		builder.Services.AddTrainerData(dbPath);

		// --- Сервисы ---
		builder.Services.AddSingleton<PinService>();

		// --- ViewModels ---
		builder.Services.AddTransient<GroupsViewModel>();
		builder.Services.AddTransient<ClientsViewModel>();

		// --- Pages ---
		builder.Services.AddTransient<PinSetupPage>();
		builder.Services.AddTransient<PinEntryPage>();
		builder.Services.AddTransient<GroupsPage>();
		builder.Services.AddTransient<ClientsPage>();
		builder.Services.AddTransient<ClientEditPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Создаём БД, если её нет.
		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
			db.Database.EnsureCreated();
		}

		return app;
	}
}
