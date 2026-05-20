using Refit;
using Trainer.App.Services;

namespace Trainer.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly IServiceProvider _services;

    public LoginPage(AuthService auth, IServiceProvider services)
    {
        InitializeComponent();
        _auth = auth;
        _services = services;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var pwd = PasswordEntry.Text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pwd))
        {
            ShowError("Введи email и пароль.");
            return;
        }

        SetBusy(true);
        try
        {
            await _auth.LoginAsync(email, pwd);
            await GoToNextPageAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            ShowError("Неверный email или пароль.");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка входа: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnAcceptInviteClicked(object? sender, EventArgs e)
    {
        var code = await DisplayPromptAsync("Код приглашения", "Введи 8-значный код от главного тренера",
            "Далее", "Отмена", maxLength: 8);
        if (string.IsNullOrWhiteSpace(code)) return;

        var pwd = await DisplayPromptAsync("Новый пароль", "Придумай пароль для этого аккаунта (мин. 6 символов)",
            "Зарегистрироваться", "Отмена", maxLength: 64);
        if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 6)
        {
            ShowError("Пароль должен быть минимум 6 символов.");
            return;
        }

        SetBusy(true);
        try
        {
            await _auth.AcceptInviteAsync(code.Trim(), pwd);
            await GoToNextPageAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            ShowError("Код недействителен или истёк.");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task GoToNextPageAsync()
    {
        // After successful auth: route to PinSetup if no PIN yet, else straight into the shell.
        var pin = _services.GetRequiredService<PinService>();
        Page next = pin.IsConfigured
            ? new AppShell()
            : _services.GetRequiredService<PinSetupPage>();
        Application.Current!.Windows[0].Page = next;
        await Task.CompletedTask;
    }

    private void SetBusy(bool busy)
    {
        LoginBtn.IsEnabled = !busy;
        AcceptInviteBtn.IsEnabled = !busy;
        Spinner.IsRunning = busy;
        Spinner.IsVisible = busy;
        if (busy) ErrorLabel.IsVisible = false;
    }

    private void ShowError(string text)
    {
        ErrorLabel.Text = text;
        ErrorLabel.IsVisible = true;
    }
}
