using Trainer.App.Services;
using Trainer.Core.Abstractions;

namespace Trainer.App.Pages;

public partial class BackupPage : ContentPage
{
    private readonly IBackupService _backup;
    private readonly PinService _pin;
    private readonly BiometricService _biometric;

    public BackupPage(IBackupService backup, PinService pin, BiometricService biometric)
    {
        InitializeComponent();
        _backup = backup;
        _pin = pin;
        _biometric = biometric;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Показываем секцию только если на устройстве реально есть биометрия.
        if (_biometric.IsAvailable())
        {
            BiometricSection.IsVisible = true;
            BiometricLabel.Text = $"{_biometric.DisplayName()} для входа";
            BiometricHint.Text = $"Вход в приложение через {_biometric.DisplayName()}. PIN останется как запасной вариант.";
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
