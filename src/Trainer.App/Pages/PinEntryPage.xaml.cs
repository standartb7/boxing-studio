using Trainer.App.Services;

namespace Trainer.App.Pages;

public partial class PinEntryPage : ContentPage
{
    private readonly PinService _pin;
    private readonly BiometricService _biometric;
    private bool _biometricAttempted;

    public PinEntryPage(PinService pin, BiometricService biometric)
    {
        InitializeComponent();
        _pin = pin;
        _biometric = biometric;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PinEntry.Text = string.Empty;

        var canUseBiometric = _pin.IsBiometricEnabled && _biometric.IsAvailable();
        BiometricBtn.IsVisible = canUseBiometric;
        BiometricBtn.Text = $"Войти через {_biometric.DisplayName()}";

        if (canUseBiometric && !_biometricAttempted)
        {
            _biometricAttempted = true;
            await TryBiometricAsync();
            // если биометрия не сработала — упадём в обычный PIN, который и так уже на экране
        }
        else
        {
            PinEntry.Focus();
        }
    }

    private async Task TryBiometricAsync()
    {
        var ok = await _biometric.AuthenticateAsync("Войти в приложение");
        if (ok)
        {
            ErrorLabel.IsVisible = false;
            SwitchToMainShell();
        }
        else
        {
            PinEntry.Focus();
        }
    }

    private async void OnBiometricClicked(object sender, EventArgs e)
    {
        await TryBiometricAsync();
    }

    private void OnPinTextChanged(object sender, TextChangedEventArgs e)
    {
        var pin = e.NewTextValue;
        if (string.IsNullOrEmpty(pin) || pin.Length < 4) return;

        if (_pin.Verify(pin))
        {
            ErrorLabel.IsVisible = false;
            SwitchToMainShell();
        }
        else
        {
            ErrorLabel.Text = "Неверный PIN";
            ErrorLabel.IsVisible = true;
            PinEntry.Text = string.Empty;
        }
    }

    private void SwitchToMainShell()
    {
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new AppShell();
    }
}
