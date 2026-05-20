using Trainer.App.Api;
using Trainer.App.Services;
using Trainer.App.ViewModels;
using Trainer.Contracts;

namespace Trainer.App.Pages;

public partial class GroupsPage : ContentPage
{
    private readonly GroupsViewModel _vm;
    private readonly IServiceProvider _services;
    private readonly AuthService _auth;
    private readonly TrainerFilterContext _filter;
    private readonly IUserApi _users;

    private List<TrainerPickerItem> _pickerItems = new();
    private bool _suppressPickerEvent;

    public GroupsPage(GroupsViewModel vm, IServiceProvider services, AuthService auth,
        TrainerFilterContext filter, IUserApi users)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        _auth = auth;
        _filter = filter;
        _users = users;
        GroupsList.ItemsSource = _vm.Items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_auth.IsHeadTrainer)
        {
            TrainerFilterBar.IsVisible = true;
            await LoadTrainerPickerAsync();
        }
        else
        {
            TrainerFilterBar.IsVisible = false;
            // Regular trainer: clear any stale filter from a previous session-state quirk.
            _filter.Reset();
        }

        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
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

            _suppressPickerEvent = true;
            TrainerPicker.ItemsSource = _pickerItems;
            TrainerPicker.ItemDisplayBinding = new Binding(nameof(TrainerPickerItem.Label));
            // Match the current filter (preserved across navigation).
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
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не удалось удалить", ex.Message, "OK");
        }
    }

    private record TrainerPickerItem(Guid? Id, string Label);
}
