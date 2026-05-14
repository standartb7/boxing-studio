using Trainer.App.Pages;
using Trainer.App.Services;

namespace Trainer.App;

public partial class App : Application
{
	private readonly PinService _pin;
	private readonly IServiceProvider _services;

	public App(PinService pin, IServiceProvider services)
	{
		InitializeComponent();
		_pin = pin;
		_services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Page rootPage = _pin.IsConfigured
			? _services.GetRequiredService<PinEntryPage>()
			: _services.GetRequiredService<PinSetupPage>();

		return new Window(rootPage);
	}
}
