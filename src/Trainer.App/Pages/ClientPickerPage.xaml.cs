using System.Collections.ObjectModel;
using Trainer.App.Services;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Pages;

/// <summary>
/// Модалка выбора клиента с поиском. Сообщает результат через колбек <see cref="Picked"/>.
/// Колбек вызывается только при явном выборе/создании; при Cancel или swipe-dismiss — не вызывается.
/// (Намеренно без TaskCompletionSource: lifecycle-хуки на iOS могут стрельнуть при показе
/// DisplayPromptAsync или анимации модалки и резолвнуть TCS в null раньше, чем юзер успеет ввести.)
/// </summary>
public partial class ClientPickerPage : ContentPage
{
    private readonly IClientService _clients;
    private readonly ObservableCollection<Client> _filtered = new();

    private List<Client> _all = new();
    private HashSet<Guid> _excludeIds = new();

    public Action<Client>? Picked { get; set; }

    public ClientPickerPage(IClientService clients)
    {
        InitializeComponent();
        _clients = clients;
        ClientsList.ItemsSource = _filtered;
    }

    /// <summary>Задаётся вызывающей стороной перед PushModalAsync — исключить уже добавленных.</summary>
    public void SetExcluded(IEnumerable<Guid> excludedIds)
    {
        _excludeIds = new HashSet<Guid>(excludedIds);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            var all = await _clients.GetAllAsync();
            _all = all.Where(c => !_excludeIds.Contains(c.Id)).ToList();
            ApplyFilter();
            ErrorBanner.IsVisible = false;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ErrorMessageHelper.Format(ex);
            ErrorBanner.IsVisible = true;
        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        ErrorBanner.IsVisible = false;
        await ReloadAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchBox.Text?.Trim() ?? string.Empty;
        _filtered.Clear();
        IEnumerable<Client> source = _all;
        if (!string.IsNullOrEmpty(q))
            source = _all.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        foreach (var c in source) _filtered.Add(c);
    }

    private async void OnClientSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Client chosen) return;
        ClientsList.SelectedItem = null;

        Picked?.Invoke(chosen);
        await Navigation.PopModalAsync();
    }

    private async void OnCreateNewClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Новый участник", "Имя:", "Далее", "Отмена",
            placeholder: "Иван Петров");
        if (name is null) return;
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ошибка", "Имя не может быть пустым", "OK");
            return;
        }

        var phone = await DisplayPromptAsync("Новый участник", "Телефон (необязательно):", "Создать", "Отмена",
            placeholder: "+7 999 000 0000", keyboard: Keyboard.Telephone);
        if (phone is null) return;

        try
        {
            var trimmedName = name.Trim();
            var trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            var created = await _clients.CreateAsync(new Client
            {
                Name = trimmedName,
                Phone = trimmedPhone,
            });

            // Server's create-or-get: if the name already existed elsewhere in the gym,
            // we get the existing record back. Phone may differ — tell the trainer so they
            // know they're attaching to an existing person, not a fresh one.
            var sameName = string.Equals(created.Name, trimmedName, StringComparison.OrdinalIgnoreCase);
            var phoneDiffers = !string.IsNullOrEmpty(trimmedPhone)
                && !string.Equals(created.Phone ?? string.Empty, trimmedPhone, StringComparison.Ordinal);
            if (sameName && phoneDiffers)
            {
                await DisplayAlert("Найден существующий клиент",
                    $"«{created.Name}» уже есть в системе (телефон: {created.Phone ?? "—"}). " +
                    "Добавляю его, твой введённый телефон не использован.", "OK");
            }

            Picked?.Invoke(created);
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ErrorMessageHelper.Format(ex), "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
