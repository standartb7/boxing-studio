using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trainer.App.Pages;
using Trainer.App.Services;
using Trainer.App.ViewModels;
using Trainer.Core.Entities;
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
		builder.Services.AddTrainerData(dbPath);

		builder.Services.AddSingleton<PinService>();
		builder.Services.AddSingleton<BiometricService>();

		// ViewModels
		builder.Services.AddTransient<GroupsViewModel>();
		builder.Services.AddTransient<SessionsViewModel>();

		// Pages
		builder.Services.AddTransient<PinSetupPage>();
		builder.Services.AddTransient<PinEntryPage>();
		builder.Services.AddTransient<GroupsPage>();
		builder.Services.AddTransient<SessionsPage>();
		builder.Services.AddTransient<SessionEditPage>();
		builder.Services.AddTransient<ClientPickerPage>();
		builder.Services.AddTransient<BackupPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
#if DEBUG_WIPE_DB
			// Включается в csproj через <DefineConstants>$(DefineConstants);DEBUG_WIPE_DB</DefineConstants>.
			// Пересоздаёт БД на каждом запуске — для смены схемы пока нет миграций.
			db.Database.EnsureDeleted();
#endif
			db.Database.EnsureCreated();

			// Сид: если типов нет — заполняем дефолтными.
			if (!db.TrainingTypes.Any())
			{
				db.TrainingTypes.AddRange(
					new TrainingType { Name = "Персональные", SortOrder = 1 },
					new TrainingType { Name = "Групповые",    SortOrder = 2 },
					new TrainingType { Name = "Детские",      SortOrder = 3 });
				db.SaveChanges();
			}
		}

		return app;
	}
}
