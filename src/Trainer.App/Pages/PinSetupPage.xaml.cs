using Trainer.App.Services;

namespace Trainer.App.Pages;

public partial class PinSetupPage : ContentPage
{
    private readonly PinService _pin;
    private readonly BiometricService _biometric;

    public PinSetupPage(PinService pin, BiometricService biometric)
    {
        InitializeComponent();
        _pin = pin;
        _biometric = biometric;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var pin = PinEntry.Text?.Trim();
        var confirm = ConfirmEntry.Text?.Trim();

        if (string.IsNullOrEmpty(pin) || pin.Length != 4)
        {
            ShowError("PIN должен быть ровно 4 цифры");
            return;
        }
        if (!pin.All(char.IsDigit))
        {
            ShowError("PIN должен состоять только из цифр");
            return;
        }
        if (pin != confirm)
        {
            ShowError("PIN-ы не совпадают");
            return;
        }

        _pin.SetPin(pin);

        // Если на устройстве есть биометрия — сразу предложим её включить.
        // Проверяем именно сейчас (через подтверждение Face ID), чтобы юзер дал системное разрешение.
        if (_biometric.IsAvailable())
        {
            var enable = await DisplayAlert(
                $"Включить {_biometric.DisplayName()}?",
                $"Можно входить в приложение через {_biometric.DisplayName()}, без PIN-кода. PIN останется как запасной вариант.",
                "Включить",
                "Не сейчас");
            if (enable)
            {
                // Сразу проверим что биометрия реально работает — заодно iOS попросит разрешение.
                var ok = await _biometric.AuthenticateAsync($"Включить {_biometric.DisplayName()}");
                _pin.IsBiometricEnabled = ok;
            }
        }

        SwitchToMainShell();
    }

    private void ShowError(string? message)
    {
        ErrorLabel.Text = message ?? string.Empty;
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(message);
    }

    private void SwitchToMainShell()
    {
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new AppShell();
    }
}
