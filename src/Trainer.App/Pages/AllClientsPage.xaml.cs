using Trainer.App.Common;
using Trainer.App.ViewModels;

namespace Trainer.App.Pages;

/// <summary>
/// HeadTrainer-only "all clients" screen — full gym roster with search, call, and a quick
/// view into which sessions / trainers each client is associated with.
///
/// We don't paginate: the API returns the whole list, we filter in-memory. CollectionView
/// virtualises rendering so this stays responsive up into the thousands of clients.
/// </summary>
public partial class AllClientsPage : ContentPage
{
    private readonly AllClientsViewModel _vm;
    private bool _loadedOnce;

    public AllClientsPage(AllClientsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        ClientsList.ItemsSource = _vm.Filtered;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loadedOnce)
        {
            _loadedOnce = true;
            await ReloadAsync(showOverlay: true);
        }
    }

    private async Task ReloadAsync(bool showOverlay)
    {
        if (showOverlay) LoadingOverlay.IsVisible = true;
        try
        {
            await _vm.LoadAsync();
            UpdateCountLabel();
            UpdateEmptyHint();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ErrorMessageHelper.Format(ex), "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        try { await ReloadAsync(showOverlay: false); }
        finally { Refresher.IsRefreshing = false; }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = e.NewTextValue ?? string.Empty;
        UpdateCountLabel();
        UpdateEmptyHint();
    }

    private void UpdateCountLabel()
    {
        CountLabel.Text = $"{_vm.FilteredCount:00} / {_vm.TotalCount:00}";
    }

    private void UpdateEmptyHint()
    {
        // Different empty-state copy for "no clients in the gym" vs "no matches for query".
        var hasQuery = !string.IsNullOrEmpty(_vm.SearchText);
        EmptyTitleLabel.Text = hasQuery ? "НИЧЕГО НЕ НАЙДЕНО" : "НЕТ КЛИЕНТОВ";
        EmptyHintLabel.Text = hasQuery
            ? "ПОПРОБУЙ ИЗМЕНИТЬ ЗАПРОС"
            : "ЗДЕСЬ ПОЯВЯТСЯ ВСЕ КЛИЕНТЫ ЗАЛА";
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCallClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ClientCard card) return;
        await TryDial(card.Client.Phone);
    }

    private async void OnClientTapped(object? sender, TappedEventArgs e)
    {
        // Tap the row → action sheet with the sessions the client belongs to + Call action.
        if (sender is not Border border || border.BindingContext is not ClientCard card) return;

        var actions = new List<string>();
        if (card.HasPhone) actions.Add($"📞 ПОЗВОНИТЬ · {card.Phone}");

        if (card.Sessions.Count == 0)
        {
            actions.Add("(нет сессий)");
        }
        else
        {
            foreach (var s in card.Sessions)
                actions.Add($"◆ {s.Title} · {s.OwnerName}");
        }

        var pick = await DisplayActionSheet(card.NameUpper, "ЗАКРЫТЬ", null, actions.ToArray());
        if (string.IsNullOrEmpty(pick) || pick == "ЗАКРЫТЬ") return;
        if (pick.StartsWith("📞")) await TryDial(card.Client.Phone);
        // Other items are informational — no nav for now (no public API to filter sessions
        // by an arbitrary client across all training types).
    }

    private async Task TryDial(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        try
        {
            PhoneDialer.Default.Open(phone.Trim());
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Не поддерживается", "На этом устройстве нет звонилки.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}
