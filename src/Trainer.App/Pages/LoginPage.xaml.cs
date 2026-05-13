using Trainer.App.Services.Auth;

namespace Trainer.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _auth;

    public LoginPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Заполни email и пароль");
            return;
        }

        SetBusy(true);
        ShowError(null);

        var result = await _auth.LoginAsync(email!, password!);

        SetBusy(false);

        if (!result.Success)
        {
            ShowError(result.ErrorMessage ?? "Ошибка входа");
            return;
        }

        SwitchToMainShell();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var registerPage = Handler!.MauiContext!.Services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }

    private void ShowError(string? message)
    {
        ErrorLabel.Text = message ?? string.Empty;
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(message);
    }

    private void SetBusy(bool busy)
    {
        Loading.IsRunning = busy;
        Loading.IsVisible = busy;
        LoginBtn.IsEnabled = !busy;
    }

    private void SwitchToMainShell()
    {
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
