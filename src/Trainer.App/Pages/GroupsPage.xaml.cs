using Trainer.App.ViewModels;

namespace Trainer.App.Pages;

public partial class GroupsPage : ContentPage
{
    private readonly GroupsViewModel _vm;
    private readonly IServiceProvider _services;

    public GroupsPage(GroupsViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        GroupsList.ItemsSource = _vm.Items;
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
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not GroupRow row) return;
        GroupsList.SelectedItem = null;

        var page = _services.GetRequiredService<ClientsPage>();
        page.Filter = row.Type;
        await Navigation.PushAsync(page);
    }
}
