using Trainer.App.Services.Auth;

namespace Trainer.App.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthService _auth;

    public RegisterPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var displayName = DisplayNameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Заполни email и пароль");
            return;
        }

        if (password!.Length < 8)
        {
            ShowError("Пароль должен быть минимум 8 символов");
            return;
        }

        SetBusy(true);
        ShowError(null);

        var result = await _auth.RegisterAsync(email!, password, displayName);

        SetBusy(false);

        if (!result.Success)
        {
            ShowError(result.ErrorMessage ?? "Ошибка регистрации");
            return;
        }

        SwitchToMainShell();
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
        RegisterBtn.IsEnabled = !busy;
    }

    private void SwitchToMainShell()
    {
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
