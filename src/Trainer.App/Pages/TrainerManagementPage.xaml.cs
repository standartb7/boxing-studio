using System.Collections.ObjectModel;
using Refit;
using Trainer.App.Api;
using Trainer.Contracts;

namespace Trainer.App.Pages;

public partial class TrainerManagementPage : ContentPage
{
    private readonly IUserApi _users;
    private readonly IAuthAdminApi _authAdmin;
    private readonly ObservableCollection<UserDto> _list = new();

    public TrainerManagementPage(IUserApi users, IAuthAdminApi authAdmin)
    {
        InitializeComponent();
        _users = users;
        _authAdmin = authAdmin;
        UsersList.ItemsSource = _list;
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
            var users = await _users.GetAllAsync();
            _list.Clear();
            foreach (var u in users.OrderBy(x => x.DisplayName)) _list.Add(u);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", FormatError(ex), "OK");
        }
    }

    private async void OnInviteClicked(object? sender, EventArgs e)
    {
        var email = await DisplayPromptAsync("Новый тренер", "Email:", "Далее", "Отмена",
            placeholder: "trainer@gym.test", keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;

        var displayName = await DisplayPromptAsync("Новый тренер", "Имя для отображения:", "Далее", "Отмена",
            placeholder: "Иван Петров");
        if (string.IsNullOrWhiteSpace(displayName)) return;

        var roleChoice = await DisplayActionSheet("Роль", "Отмена", null, "Тренер", "Главный тренер");
        if (roleChoice == "Отмена" || string.IsNullOrEmpty(roleChoice)) return;
        var role = roleChoice == "Главный тренер" ? "HeadTrainer" : "Trainer";

        try
        {
            InviteBtn.IsEnabled = false;
            var resp = await _authAdmin.InviteAsync(new InviteRequest
            {
                Email = email.Trim(),
                DisplayName = displayName.Trim(),
                Role = role,
            });

            await DisplayAlert("Код приглашения",
                $"Передай этот код тренеру лично — он введёт его на экране входа:\n\n{resp.InviteCode}\n\nДействует до {resp.ExpiresAt.LocalDateTime:dd.MM.yyyy}.",
                "OK");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не получилось пригласить", FormatError(ex), "OK");
        }
        finally
        {
            InviteBtn.IsEnabled = true;
        }
    }

    private async void OnUserTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bo || bo.BindingContext is not UserDto user) return;

        var actions = new List<string>();
        var toggleRoleLabel = user.Role == "HeadTrainer"
            ? "Сделать обычным тренером"
            : "Сделать главным тренером";
        actions.Add(toggleRoleLabel);
        actions.Add(user.IsActive ? "Деактивировать" : "Активировать");

        var choice = await DisplayActionSheet(user.DisplayName, "Отмена", null, actions.ToArray());
        if (choice == "Отмена" || string.IsNullOrEmpty(choice)) return;

        try
        {
            if (choice == toggleRoleLabel)
            {
                var newRole = user.Role == "HeadTrainer" ? "Trainer" : "HeadTrainer";
                await _users.UpdateAsync(user.Id, new UpdateUserRequest { Role = newRole });
            }
            else
            {
                await _users.UpdateAsync(user.Id, new UpdateUserRequest { IsActive = !user.IsActive });
            }
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не получилось", FormatError(ex), "OK");
        }
    }

    private static string FormatError(Exception ex)
    {
        if (ex is ApiException api)
            return string.IsNullOrWhiteSpace(api.Content) ? $"HTTP {(int)api.StatusCode}" : api.Content;
        return ex.Message;
    }
}
