using Microsoft.Extensions.Logging;
using Trainer.App.Pages;
using Trainer.App.Services.Api;
using Trainer.App.Services.Auth;
using Trainer.App.ViewModels;
using Trainer.Core.Abstractions;

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

		// --- Auth state (singleton) ---
		builder.Services.AddSingleton<AuthState>();
		builder.Services.AddSingleton<IAuthService, AuthService>();

		// --- HTTP clients ---
		// "auth" — без bearer handler-а (запросы register/login до получения токена)
		builder.Services.AddHttpClient("auth", c => c.BaseAddress = new Uri(ApiSettings.BaseUrl));

		// "api" — с автоматической подстановкой Bearer-токена
		builder.Services.AddTransient<BearerTokenHandler>();
		builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(ApiSettings.BaseUrl))
			.AddHttpMessageHandler<BearerTokenHandler>();

		// --- Доменные сервисы: HTTP-реализации интерфейсов из Trainer.Core ---
		builder.Services.AddSingleton<IClientService, ApiClientService>();
		// TODO: IExerciseService, IWorkoutTemplateService, IScheduleService — когда напишем endpoints

		// --- Pages + ViewModels ---
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<ClientsPage>();
		builder.Services.AddTransient<ClientsViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
