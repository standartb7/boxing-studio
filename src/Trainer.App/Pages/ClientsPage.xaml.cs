using Trainer.App.ViewModels;
using Trainer.Core.Entities;

namespace Trainer.App.Pages;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _vm;
    private readonly IServiceProvider _services;

    public ClientsPage(ClientsViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        ClientsList.ItemsSource = _vm.Items;
    }

    public void SetFilter(Guid trainingTypeId, string typeName)
    {
        _vm.FilterTypeId = trainingTypeId;
        Title = typeName;
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

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var editor = _services.GetRequiredService<ClientEditPage>();
        editor.SetClient(null, _vm.FilterTypeId);
        await Navigation.PushAsync(editor);
    }

    private async void OnClientSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Client client) return;
        ClientsList.SelectedItem = null;

        var editor = _services.GetRequiredService<ClientEditPage>();
        editor.SetClient(client, _vm.FilterTypeId);
        await Navigation.PushAsync(editor);
    }
}
