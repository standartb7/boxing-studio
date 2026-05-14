using Trainer.App.Services;

namespace Trainer.App.Pages;

public partial class PinSetupPage : ContentPage
{
    private readonly PinService _pin;

    public PinSetupPage(PinService pin)
    {
        InitializeComponent();
        _pin = pin;
    }

    private void OnSaveClicked(object sender, EventArgs e)
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
