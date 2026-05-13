using Trainer.App.Pages;
using Trainer.App.Services.Auth;

namespace Trainer.App;

public partial class App : Application
{
	private readonly AuthState _authState;
	private readonly IServiceProvider _services;

	public App(AuthState authState, IServiceProvider services)
	{
		InitializeComponent();
		_authState = authState;
		_services = services;

		_authState.Changed += OnAuthChanged;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// При старте читаем сохранённый токен. Делаем синхронно через .GetAwaiter().GetResult()
		// — SecureStorage async-only, но на старте окна это норм.
		_authState.LoadAsync().GetAwaiter().GetResult();

		Page rootPage = _authState.IsAuthenticated
			? new AppShell()
			: BuildLoginNavigation();

		return new Window(rootPage);
	}

	private void OnAuthChanged(object? sender, EventArgs e)
	{
		if (Windows.Count == 0) return;
		var window = Windows[0];

		MainThread.BeginInvokeOnMainThread(() =>
		{
			window.Page = _authState.IsAuthenticated
				? new AppShell()
				: BuildLoginNavigation();
		});
	}

	private NavigationPage BuildLoginNavigation()
	{
		var login = _services.GetRequiredService<LoginPage>();
		return new NavigationPage(login);
	}
}
