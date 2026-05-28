using Trainer.App.Services;
using Trainer.App.State;
using Trainer.Core.Abstractions;

namespace Trainer.App.Pages;

public partial class BackupPage : ContentPage
{
    private readonly IBackupService _backup;
    private readonly PinService _pin;
    private readonly BiometricService _biometric;
    private readonly AuthService _auth;
    private readonly IServiceProvider _services;

    public BackupPage(IBackupService backup, PinService pin, BiometricService biometric,
        AuthService auth, IServiceProvider services)
    {
        InitializeComponent();
        _backup = backup;
        _pin = pin;
        _biometric = biometric;
        _auth = auth;
        _services = services;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Account section: who am I + admin-only "Manage trainers" button.
        var displayName = _auth.CurrentUserDisplayName ?? _auth.CurrentUserEmail ?? "—";
        AccountLabel.Text = displayName.ToUpperInvariant();
        AccountRoleLabel.Text = _auth.IsHeadTrainer ? "◆ ГЛАВНЫЙ ТРЕНЕР · HEAD COACH" : "◆ ТРЕНЕР · CORNERMAN";
        AccountInitialsLabel.Text = MakeInitials(displayName);
        TrainersBtn.IsVisible = _auth.IsHeadTrainer;

        // Показываем секцию только если на устройстве реально есть биометрия.
        if (_biometric.IsAvailable())
        {
            BiometricSection.IsVisible = true;
            BiometricLabel.Text = $"{_biometric.DisplayName().ToUpperInvariant()} ДЛЯ ВХОДА";
            BiometricHint.Text = $"ВХОД В ПРИЛОЖЕНИЕ ЧЕРЕЗ {_biometric.DisplayName().ToUpperInvariant()}. PIN ОСТАНЕТСЯ КАК ЗАПАСНОЙ ВАРИАНТ.";
            // Подписку временно отключаем чтобы программное изменение не дёрнуло Toggled.
            BiometricSwitch.Toggled -= OnBiometricToggled;
            BiometricSwitch.IsToggled = _pin.IsBiometricEnabled;
            BiometricSwitch.Toggled += OnBiometricToggled;
        }
        else
        {
            BiometricSection.IsVisible = false;
        }
    }

    private async void OnBiometricToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // Включаем — потребуем подтверждения Face ID/Touch ID, чтобы убедиться,
            // что юзер реально может им пользоваться (иначе залочится).
            var ok = await _biometric.AuthenticateAsync($"Включить {_biometric.DisplayName()}");
            if (ok)
            {
                _pin.IsBiometricEnabled = true;
            }
            else
            {
                _pin.IsBiometricEnabled = false;
                // Откатываем switch без триггера событий.
                BiometricSwitch.Toggled -= OnBiometricToggled;
                BiometricSwitch.IsToggled = false;
                BiometricSwitch.Toggled += OnBiometricToggled;
            }
        }
        else
        {
            _pin.IsBiometricEnabled = false;
        }
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        ExportBtn.IsEnabled = false;
        SetStatus(null);

        try
        {
            var json = await _backup.ExportJsonAsync();

            // CacheDirectory чтобы iOS сам подчищал — это не данные пользователя,
            // а временный файл для шеринга.
            var stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
            var path = Path.Combine(FileSystem.CacheDirectory, $"boxing-studio-backup-{stamp}.json");
            await File.WriteAllTextAsync(path, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Бэкап Boxing Studio",
                File = new ShareFile(path),
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка экспорта", ex.Message, "OK");
        }
        finally
        {
            ExportBtn.IsEnabled = true;
        }
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        ImportBtn.IsEnabled = false;
        SetStatus(null);

        try
        {
            // На iOS .json иногда не входит в стандартный фильтр — разрешаем item, чтобы
            // юзер мог выбрать файл независимо от того, как он туда попал (Files, iCloud, Mail).
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS,        new[] { "public.json", "public.text", "public.item" } },
                { DevicePlatform.macOS,      new[] { "json", "txt" } },
                { DevicePlatform.Android,    new[] { "application/json", "text/plain", "*/*" } },
                { DevicePlatform.WinUI,      new[] { ".json", ".txt" } },
            });

            var pick = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выбери файл бэкапа",
                FileTypes = fileTypes,
            });
            if (pick is null) return;

            string json;
            using (var stream = await pick.OpenReadAsync())
            using (var reader = new StreamReader(stream))
                json = await reader.ReadToEndAsync();

            var ok = await DisplayAlert(
                "Импорт",
                "Все текущие группы, клиенты и сессии будут ЗАМЕНЕНЫ на данные из файла. Продолжить?",
                "Заменить",
                "Отмена");
            if (!ok) return;

            await _backup.ImportJsonAsync(json);

            await DisplayAlert("Готово", "Данные импортированы. Вернись на главный экран — увидишь обновлённый список.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка импорта", ex.Message, "OK");
        }
        finally
        {
            ImportBtn.IsEnabled = true;
        }
    }

    private async void OnManageTrainersClicked(object? sender, EventArgs e)
    {
        var page = _services.GetRequiredService<TrainerManagementPage>();
        await Navigation.PushAsync(page);
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        var page = _services.GetRequiredService<ChangePasswordPage>();
        await Navigation.PushAsync(page);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private static string MakeInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "—";
        if (parts.Length == 1) return parts[0].Length > 0 ? parts[0][..1].ToUpperInvariant() : "—";
        return string.Concat(parts[0][..1], parts[1][..1]).ToUpperInvariant();
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlert("Выйти?",
            "PIN и Face ID на этом устройстве будут сброшены — для следующего входа понадобятся email и пароль.",
            "Выйти", "Отмена");
        if (!ok) return;

        try { await _auth.LogoutAsync(); } catch { /* offline — wipe locally anyway */ }
        _pin.Reset();
        _services.GetRequiredService<TrainerFilterContext>().Reset();

        if (Application.Current?.Windows.Count > 0)
        {
            var login = _services.GetRequiredService<LoginPage>();
            Application.Current.Windows[0].Page = login;
        }
    }

    private void SetStatus(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            StatusLabel.IsVisible = false;
            StatusLabel.Text = string.Empty;
        }
        else
        {
            StatusLabel.Text = text;
            StatusLabel.IsVisible = true;
        }
    }
}
