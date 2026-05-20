using Trainer.App.Pages;
using Trainer.App.Services;

namespace Trainer.App;

public partial class App : Application
{
	private readonly AuthService _auth;
	private readonly PinService _pin;
	private readonly IServiceProvider _services;

	public App(AuthService auth, PinService pin, IServiceProvider services)
	{
		InitializeComponent();
		_auth = auth;
		_pin = pin;
		_services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Page rootPage;
		if (!_auth.IsLoggedIn)
		{
			// First launch (or after sign-out) → cloud login.
			rootPage = _services.GetRequiredService<LoginPage>();
		}
		else if (_pin.IsConfigured)
		{
			// Returning user with PIN/Face ID → quick unlock.
			rootPage = _services.GetRequiredService<PinEntryPage>();
		}
		else
		{
			// Already logged in but no PIN yet (e.g. PIN was reset) → set up local lock.
			rootPage = _services.GetRequiredService<PinSetupPage>();
		}

		return new Window(rootPage);
	}
}
