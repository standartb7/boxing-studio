using System.Collections.ObjectModel;
using Refit;
using Trainer.App.Api;
using Trainer.App.Services;
using Trainer.Contracts;
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
    private readonly IClientApi _clientApi;
    private readonly IUserApi _userApi;
    private readonly AuthService _auth;
    private readonly ObservableCollection<Client> _filtered = new();

    private List<Client> _all = new();
    private HashSet<Guid> _excludeIds = new();

    public Action<Client>? Picked { get; set; }

    public ClientPickerPage(IClientService clients, IClientApi clientApi, IUserApi userApi, AuthService auth)
    {
        InitializeComponent();
        _clients = clients;
        _clientApi = clientApi;
        _userApi = userApi;
        _auth = auth;
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
            var filtered = all.Where(c => !_excludeIds.Contains(c.Id)).ToList();

            // Hide the owner-name line for regular trainers — they only ever see their own
            // clients, so showing "тренер: их собственное имя" under every row is noise.
            // The reassign swipe action is HeadTrainer-only too.
            var isHead = _auth.IsHeadTrainer;
            foreach (var c in filtered)
            {
                if (!isHead) c.OwnerDisplayName = null;
                c.CanReassign = isHead;
            }

            _all = filtered;
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

    private async void OnReassignSwipe(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem si || si.BindingContext is not Client client) return;

        List<UserDto> trainers;
        try
        {
            trainers = (await _userApi.GetAllAsync())
                .Where(u => u.IsActive && u.Id != client.OwnerTrainerId)
                .OrderBy(u => u.DisplayName)
                .ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не получилось загрузить список тренеров", FormatError(ex), "OK");
            return;
        }

        if (trainers.Count == 0)
        {
            await DisplayAlert("Нет тренеров", "Нет других активных тренеров для передачи клиента.", "OK");
            return;
        }

        var names = trainers.Select(t => t.DisplayName).ToArray();
        var picked = await DisplayActionSheet($"Передать «{client.Name}»", "Отмена", null, names);
        if (picked == "Отмена" || string.IsNullOrEmpty(picked)) return;

        var target = trainers.First(t => t.DisplayName == picked);
        try
        {
            var updated = await _clientApi.ReassignAsync(client.Id,
                new ReassignClientRequest { NewOwnerTrainerId = target.Id });

            client.OwnerTrainerId = updated.OwnerTrainerId;
            client.OwnerDisplayName = updated.OwnerDisplayName;
            ApplyFilter();  // re-bind so the owner line refreshes
            await DisplayAlert("Готово", $"«{client.Name}» теперь у тренера «{target.DisplayName}».", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не получилось передать", FormatError(ex), "OK");
        }
    }

    private static string FormatError(Exception ex)
    {
        if (ex is ApiException api)
            return string.IsNullOrWhiteSpace(api.Content) ? $"HTTP {(int)api.StatusCode}" : api.Content;
        return ex.Message;
    }
}
