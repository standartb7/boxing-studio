using Trainer.App.ViewModels;
using Trainer.Core.Entities;

namespace Trainer.App.Pages;

public partial class SessionsPage : ContentPage
{
    private readonly SessionsViewModel _vm;
    private readonly IServiceProvider _services;

    public SessionsPage(SessionsViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        SessionsList.ItemsSource = _vm.Items;
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
        if (_vm.FilterTypeId is null) return;
        var editor = _services.GetRequiredService<SessionEditPage>();
        editor.SetSession(null, _vm.FilterTypeId);
        await Navigation.PushAsync(editor);
    }

    private async void OnSessionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Session session) return;
        SessionsList.SelectedItem = null;

        var editor = _services.GetRequiredService<SessionEditPage>();
        editor.SetSession(session, _vm.FilterTypeId);
        await Navigation.PushAsync(editor);
    }

    private async void OnDeleteSessionInvoked(object sender, EventArgs e)
    {
        if (sender is not SwipeItem swipe || swipe.BindingContext is not Session session) return;

        var confirm = await DisplayAlert(
            "Удалить сессию?",
            $"Удалить «{session.DisplayTitle}»?\nКлиенты останутся, только связь с этой сессией пропадёт.",
            "Удалить",
            "Отмена");
        if (!confirm) return;

        try
        {
            await _vm.DeleteAsync(session.Id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не удалось удалить", ex.Message, "OK");
        }
    }
}
