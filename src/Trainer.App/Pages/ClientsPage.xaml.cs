using Trainer.App.Services.Auth;
using Trainer.App.ViewModels;

namespace Trainer.App.Pages;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _vm;
    private readonly IAuthService _auth;

    public ClientsPage(ClientsViewModel vm, IAuthService auth)
    {
        InitializeComponent();
        _vm = vm;
        _auth = auth;
        ClientsList.ItemsSource = _vm.Items;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Выход", "Выйти из аккаунта?", "Да", "Отмена");
        if (!confirm) return;
        await _auth.LogoutAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "Новый клиент",
            "Имя клиента:",
            accept: "Создать",
            cancel: "Отмена",
            placeholder: "Иван Петров");

        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            await _vm.AddAsync(name.Trim());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}
