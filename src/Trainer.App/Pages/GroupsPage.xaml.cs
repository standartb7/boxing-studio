using Trainer.App.Api;
using Trainer.App.Common;
using Trainer.App.Services;
using Trainer.App.State;
using Trainer.App.ViewModels;
using Trainer.Contracts;
using Trainer.Core.Abstractions;

namespace Trainer.App.Pages;

public partial class GroupsPage : ContentPage
{
    private readonly GroupsViewModel _vm;
    private readonly IServiceProvider _services;
    private readonly AuthService _auth;
    private readonly TrainerFilterContext _filter;
    private readonly IUserApi _users;
    private readonly IClientService _clients;

    private List<TrainerPickerItem> _pickerItems = new();
    private bool _suppressPickerEvent;

    public GroupsPage(GroupsViewModel vm, IServiceProvider services, AuthService auth,
        TrainerFilterContext filter, IUserApi users, IClientService clients)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        _auth = auth;
        _filter = filter;
        _users = users;
        _clients = clients;
        GroupsList.ItemsSource = _vm.Items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ApplyHeroFromAuth();

        if (_auth.IsHeadTrainer)
        {
            TrainerFilterBar.IsVisible = true;
            await LoadTrainerPickerAsync();
        }
        else
        {
            TrainerFilterBar.IsVisible = false;
            _filter.Reset();
        }

        await ReloadAsync();
    }

    private void ApplyHeroFromAuth()
    {
        var displayName = _auth.CurrentUserDisplayName ?? _auth.CurrentUserEmail ?? "—";
        var role = _auth.IsHeadTrainer ? "ГЛАВНЫЙ ТРЕНЕР" : "ТРЕНЕР";
        var year = DateTime.Now.Year;

        HeroRoleMonoLabel.Text = $"{role} · CH/{year}";
        HeroDisplayLabel.Text = _auth.IsHeadTrainer ? "HEAD\nCOACH" : "CORNER\nMAN";
        HeroNameLabel.Text = displayName.ToUpperInvariant();
        HeroSubLabel.Text = _auth.IsHeadTrainer
            ? "BOXING STUDIO · HEAD COACH"
            : "BOXING STUDIO · CORNERMAN";
        HeroInitialsLabel.Text = MakeInitials(displayName);
    }

    private static string MakeInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "—";
        if (parts.Length == 1) return parts[0].Length > 0 ? parts[0][..1].ToUpperInvariant() : "—";
        return string.Concat(parts[0][..1], parts[1][..1]).ToUpperInvariant();
    }

    private async Task ReloadAsync()
    {
        try
        {
            await _vm.LoadAsync();
            await RefreshStatsAsync();
            UpdateGroupCountLabel();
            UpdateLastSyncLabel();
            HideError();
        }
        catch (Exception ex)
        {
            ShowError(ErrorMessageHelper.Format(ex));
        }
    }

    private async Task RefreshStatsAsync()
    {
        StatGroupsLabel.Text = _vm.Items.Count.ToString("00");
        StatSessionsLabel.Text = _vm.TotalSessionCount.ToString("00");

        try
        {
            var allClients = await _clients.GetAllAsync();
            StatClientsLabel.Text = allClients.Count.ToString("00");
        }
        catch
        {
            // Stats are decorative — don't blow up the page if clients can't load.
            StatClientsLabel.Text = "—";
        }
    }

    private void UpdateGroupCountLabel()
    {
        var n = _vm.Items.Count.ToString("00");
        GroupCountLabel.Text = $"{n} / {n}";
    }

    private void UpdateLastSyncLabel()
    {
        LastSyncLabel.Text = $"LAST SYNC · {DateTime.Now:HH:mm}";
    }

    private void ShowError(string text)
    {
        ErrorText.Text = text;
        ErrorBanner.IsVisible = true;
    }

    private void HideError()
    {
        ErrorBanner.IsVisible = false;
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        HideError();
        await ReloadAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        try
        {
            await ReloadAsync();
        }
        finally
        {
            Refresher.IsRefreshing = false;
        }
    }

    private async Task LoadTrainerPickerAsync()
    {
        try
        {
            var trainers = (await _users.GetAllAsync())
                .Where(u => u.IsActive)
                .OrderBy(u => u.DisplayName)
                .ToList();

            _pickerItems = new List<TrainerPickerItem>
            {
                new(null, "Все тренеры"),
            };
            foreach (var t in trainers)
                _pickerItems.Add(new TrainerPickerItem(t.Id, t.DisplayName));

            if (!_filter.IsInitialized && _auth.CurrentUserId is { } selfId)
            {
                var selfName = trainers.FirstOrDefault(t => t.Id == selfId)?.DisplayName
                    ?? _auth.CurrentUserDisplayName
                    ?? "Я";
                _filter.Select(selfId, selfName);
            }

            _suppressPickerEvent = true;
            TrainerPicker.ItemsSource = _pickerItems;
            TrainerPicker.ItemDisplayBinding = new Binding(nameof(TrainerPickerItem.Label));
            var currentIdx = _pickerItems.FindIndex(i => i.Id == _filter.SelectedOwnerTrainerId);
            TrainerPicker.SelectedIndex = currentIdx < 0 ? 0 : currentIdx;
            _suppressPickerEvent = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не удалось загрузить тренеров", ex.Message, "OK");
        }
    }

    private async void OnTrainerPickerChanged(object? sender, EventArgs e)
    {
        if (_suppressPickerEvent) return;
        if (TrainerPicker.SelectedItem is not TrainerPickerItem item) return;

        _filter.Select(item.Id, item.Label);
        await ReloadAsync();
    }

    private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not GroupRow row) return;
        GroupsList.SelectedItem = null;

        var page = _services.GetRequiredService<SessionsPage>();
        page.SetFilter(row.Type.Id, row.Type.Name);
        await Navigation.PushAsync(page);
    }

    private async void OnAddGroupClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "Новая группа",
            "Название группы:",
            accept: "Создать",
            cancel: "Отмена",
            placeholder: "Например: Утренние групповые");

        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await _vm.AddGroupAsync(name.Trim());
            await RefreshStatsAsync();
            UpdateGroupCountLabel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не удалось создать", ex.Message, "OK");
        }
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        var page = _services.GetRequiredService<BackupPage>();
        await Navigation.PushAsync(page);
    }

    private async void OnDeleteGroupInvoked(object sender, EventArgs e)
    {
        if (sender is not SwipeItem swipe || swipe.BindingContext is not GroupRow row) return;

        var confirm = await DisplayAlert(
            "Удалить группу?",
            $"Удалить «{row.Type.Name}»?",
            "Удалить",
            "Отмена");
        if (!confirm) return;

        try
        {
            await _vm.DeleteGroupAsync(row.Type.Id);
            await RefreshStatsAsync();
            UpdateGroupCountLabel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не удалось удалить", ex.Message, "OK");
        }
    }

    private record TrainerPickerItem(Guid? Id, string Label);
}
