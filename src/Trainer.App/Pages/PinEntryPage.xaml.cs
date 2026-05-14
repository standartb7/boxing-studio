using Trainer.App.Services;

namespace Trainer.App.Pages;

public partial class PinEntryPage : ContentPage
{
    private readonly PinService _pin;

    public PinEntryPage(PinService pin)
    {
        InitializeComponent();
        _pin = pin;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PinEntry.Text = string.Empty;
        PinEntry.Focus();
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
