using Refit;
using Trainer.App.Api;
using Trainer.Contracts;

namespace Trainer.App.Pages;

public partial class ChangePasswordPage : ContentPage
{
    private readonly IAuthMeApi _api;

    public ChangePasswordPage(IAuthMeApi api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var current = CurrentEntry.Text ?? string.Empty;
        var next = NewEntry.Text ?? string.Empty;
        var confirm = ConfirmEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(current))
        {
            ShowError("Введи текущий пароль.");
            return;
        }
        if (next.Length < 8)
        {
            ShowError("Новый пароль должен быть не короче 8 символов.");
            return;
        }
        if (next != confirm)
        {
            ShowError("Новые пароли не совпадают.");
            return;
        }
        if (next == current)
        {
            ShowError("Новый пароль должен отличаться от текущего.");
            return;
        }

        SetBusy(true);
        try
        {
            await _api.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = current,
                NewPassword = next,
            });
            await DisplayAlert("Готово", "Пароль обновлён.", "OK");
            await Navigation.PopAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // Сервер кладёт человекочитаемое сообщение в { "error": "..." }.
            ShowError(ParseServerError(ex) ?? "Не удалось сменить пароль.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            ShowError("Сессия истекла — перезайди в приложение.");
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

    private static string? ParseServerError(ApiException ex)
    {
        if (string.IsNullOrEmpty(ex.Content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(ex.Content);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == System.Text.Json.JsonValueKind.String)
                return err.GetString();
        }
        catch { /* not JSON — fall through */ }
        return null;
    }

    private void SetBusy(bool busy)
    {
        SaveBtn.IsEnabled = !busy;
        Spinner.IsRunning = busy;
        Spinner.IsVisible = busy;
        if (busy) ErrorLabel.IsVisible = false;
    }

    private void ShowError(string text)
    {
        ErrorLabel.Text = text;
        ErrorLabel.IsVisible = true;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
