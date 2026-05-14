using System.Collections.ObjectModel;
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
        try
        {
            var all = await _clients.GetAllAsync();
            _all = all.Where(c => !_excludeIds.Contains(c.Id)).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
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
            var created = await _clients.CreateAsync(new Client
            {
                Name = name.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            });
            Picked?.Invoke(created);
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
