using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trainer.App.Pages;
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

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "trainer.db");
		builder.Services.AddTrainerData(dbPath);

		builder.Services.AddTransient<ClientsViewModel>();
		builder.Services.AddTransient<ClientsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
			db.Database.EnsureCreated();
		}

		return app;
	}
}
