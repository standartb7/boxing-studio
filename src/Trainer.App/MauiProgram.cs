using System.Text.Json;
using Microsoft.Extensions.Logging;
using Refit;
using Trainer.App.Api;
using Trainer.App.Pages;
using Trainer.App.Services;
using Trainer.App.ViewModels;
using Trainer.Core.Abstractions;

namespace Trainer.App;

public static class MauiProgram
{
	// Hard-coded for now; if you need to point at a different backend (staging, local),
	// override at build time via a partial class / preprocessor define.
	private const string ApiBaseUrl = "https://boxing-studio.onrender.com";

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

		// --- Auth + HTTP -----------------------------------------------------
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddTransient<AuthDelegatingHandler>();

		// Explicit JSON settings — MAUI trims unused converters from System.Text.Json by
		// default, which can drop TimeOnly/DateOnly support. Register an explicit
		// JsonSerializerOptions and reuse it across all Refit clients.
		var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			PropertyNameCaseInsensitive = true,
		};
		jsonOptions.Converters.Add(new TimeOnlyJsonConverter());
		var refitSettings = new RefitSettings(new SystemTextJsonContentSerializer(jsonOptions));

		// Named HttpClient reused by HttpBackupService for raw JSON transport.
		builder.Services.AddHttpClient(HttpBackupService.HttpClientName, c =>
			{
				c.BaseAddress = new Uri(ApiBaseUrl);
			})
			.AddHttpMessageHandler<AuthDelegatingHandler>();

		// IAuthApi (login / refresh / logout / accept-invite) is anonymous — no Bearer
		// header — and lives inside AuthDelegatingHandler's loop. Registering it WITH the
		// handler would create a DI cycle (handler → AuthService → IAuthApi → handler).
		builder.Services.AddRefitClient<IAuthApi>(refitSettings)
			.ConfigureHttpClient(c => c.BaseAddress = new Uri(ApiBaseUrl));

		// Authenticated Refit clients — each goes through the same delegating handler.
		void AddAuthApi<T>() where T : class =>
			builder.Services.AddRefitClient<T>(refitSettings)
				.ConfigureHttpClient(c => c.BaseAddress = new Uri(ApiBaseUrl))
				.AddHttpMessageHandler<AuthDelegatingHandler>();

		AddAuthApi<IClientApi>();
		AddAuthApi<ISessionApi>();
		AddAuthApi<ITrainingTypeApi>();
		AddAuthApi<IUserApi>();
		AddAuthApi<IAuthAdminApi>();

		// IClientService etc. now talk to the API. ViewModels are unchanged.
		builder.Services.AddScoped<IClientService, HttpClientService>();
		builder.Services.AddScoped<ISessionService, HttpSessionService>();
		builder.Services.AddScoped<ITrainingTypeService, HttpTrainingTypeService>();
		builder.Services.AddScoped<IBackupService, HttpBackupService>();

		// Local-only services (PIN, biometric)
		builder.Services.AddSingleton<PinService>();
		builder.Services.AddSingleton<BiometricService>();

		// ViewModels
		builder.Services.AddTransient<GroupsViewModel>();
		builder.Services.AddTransient<SessionsViewModel>();

		// Pages
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<PinSetupPage>();
		builder.Services.AddTransient<PinEntryPage>();
		builder.Services.AddTransient<GroupsPage>();
		builder.Services.AddTransient<SessionsPage>();
		builder.Services.AddTransient<SessionEditPage>();
		builder.Services.AddTransient<ClientPickerPage>();
		builder.Services.AddTransient<BackupPage>();
		builder.Services.AddTransient<TrainerManagementPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
