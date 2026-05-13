using Trainer.App.ViewModels;

namespace Trainer.App.Pages;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _vm;

    public ClientsPage(ClientsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        ClientsList.ItemsSource = _vm.Items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
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
        await _vm.AddAsync(name.Trim());
    }
}
