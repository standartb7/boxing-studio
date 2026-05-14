using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Pages;

public partial class ClientEditPage : ContentPage
{
    private readonly IClientService _clients;
    private readonly ITrainingTypeService _types;

    private Client? _editing;
    private List<TrainingType> _availableTypes = new();

    // Слоты в форме хранятся отдельно от Entity, обмен только на Save.
    private readonly List<SlotRow> _slots = new();

    public ClientEditPage(IClientService clients, ITrainingTypeService types)
    {
        InitializeComponent();
        _clients = clients;
        _types = types;
    }

    public void SetClient(Client? client, Guid? defaultTypeId)
    {
        _editing = client;
        _pendingDefaultTypeId = defaultTypeId;
    }

    private Guid? _pendingDefaultTypeId;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _availableTypes = (await _types.GetAllAsync()).ToList();
            TypePicker.ItemsSource = _availableTypes;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
            return;
        }

        if (_editing is null)
        {
            Title = "Новый клиент";
            DeleteBtn.IsVisible = false;
            LastNameEntry.Text = string.Empty;
            FirstNameEntry.Text = string.Empty;
            PhoneEntry.Text = string.Empty;
            NotesEditor.Text = string.Empty;

            var defaultType = _availableTypes.FirstOrDefault(t => t.Id == _pendingDefaultTypeId)
                ?? _availableTypes.FirstOrDefault();
            TypePicker.SelectedItem = defaultType;

            _slots.Clear();
            RebuildSlotsPanel();
        }
        else
        {
            Title = _editing.FullName;
            DeleteBtn.IsVisible = true;
            LastNameEntry.Text = _editing.LastName;
            FirstNameEntry.Text = _editing.FirstName;
            PhoneEntry.Text = _editing.Phone;
            NotesEditor.Text = _editing.Notes;

            TypePicker.SelectedItem = _availableTypes.FirstOrDefault(t => t.Id == _editing.TrainingTypeId);

            _slots.Clear();
            foreach (var s in _editing.Schedule.OrderBy(s => Client.DayOrder(s.Day)).ThenBy(s => s.Time))
                _slots.Add(new SlotRow { Day = s.Day, Time = s.Time });
            RebuildSlotsPanel();
        }
    }

    private void OnAddSlotClicked(object sender, EventArgs e)
    {
        _slots.Add(new SlotRow { Day = DayOfWeek.Monday, Time = new TimeOnly(19, 0) });
        RebuildSlotsPanel();
    }

    private void RebuildSlotsPanel()
    {
        SlotsPanel.Children.Clear();
        if (_slots.Count == 0)
        {
            SlotsPanel.Children.Add(new Label
            {
                Text = "Нет слотов. Нажми «+ День», чтобы добавить.",
                FontSize = 13,
                TextColor = Colors.Gray,
            });
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            SlotsPanel.Children.Add(BuildSlotRow(slot));
        }
    }

    private View BuildSlotRow(SlotRow slot)
    {
        var dayPicker = new Picker
        {
            ItemsSource = Enum.GetValues<DayOfWeek>()
                .OrderBy(Client.DayOrder)
                .Cast<object>().ToList(),
            SelectedItem = slot.Day,
            HorizontalOptions = LayoutOptions.Fill,
        };
        dayPicker.ItemDisplayBinding = new Binding(".", converter: new DayDisplayConverter());
        dayPicker.SelectedIndexChanged += (_, _) =>
        {
            if (dayPicker.SelectedItem is DayOfWeek d) slot.Day = d;
        };

        var timePicker = new TimePicker
        {
            Time = slot.Time.ToTimeSpan(),
            Format = "HH:mm",
            WidthRequest = 100,
        };
        timePicker.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TimePicker.Time))
                slot.Time = new TimeOnly(timePicker.Time.Hours, timePicker.Time.Minutes);
        };

        var removeBtn = new Button
        {
            Text = "✕",
            FontSize = 16,
            Padding = new Thickness(10, 4),
            MinimumHeightRequest = 32,
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red,
            BorderWidth = 0,
        };
        removeBtn.Clicked += (_, _) =>
        {
            _slots.Remove(slot);
            RebuildSlotsPanel();
        };

        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            },
            ColumnSpacing = 8,
            Children = { dayPicker, timePicker, removeBtn },
        }
        .WithGridChildren(dayPicker, 0, timePicker, 1, removeBtn, 2);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var lastName = LastNameEntry.Text?.Trim() ?? string.Empty;
        var firstName = FirstNameEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            await DisplayAlert("Ошибка", "Укажи имя или фамилию", "OK");
            return;
        }
        if (TypePicker.SelectedItem is not TrainingType type)
        {
            await DisplayAlert("Ошибка", "Выбери группу", "OK");
            return;
        }

        try
        {
            if (_editing is null)
            {
                var client = new Client
                {
                    LastName = lastName,
                    FirstName = firstName,
                    Phone = PhoneEntry.Text?.Trim(),
                    Notes = NotesEditor.Text?.Trim(),
                    TrainingTypeId = type.Id,
                    Schedule = _slots.Select(s => new ScheduleSlot { Day = s.Day, Time = s.Time }).ToList(),
                };
                await _clients.CreateAsync(client);
            }
            else
            {
                _editing.LastName = lastName;
                _editing.FirstName = firstName;
                _editing.Phone = PhoneEntry.Text?.Trim();
                _editing.Notes = NotesEditor.Text?.Trim();
                _editing.TrainingTypeId = type.Id;
                _editing.Schedule = _slots.Select(s => new ScheduleSlot { ClientId = _editing.Id, Day = s.Day, Time = s.Time }).ToList();
                await _clients.UpdateAsync(_editing);
            }

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка сохранения", ex.Message, "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_editing is null) return;
        var confirm = await DisplayAlert("Удалить клиента?", $"Удалить «{_editing.FullName}»?", "Удалить", "Отмена");
        if (!confirm) return;

        try
        {
            await _clients.DeleteAsync(_editing.Id);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private class SlotRow
    {
        public DayOfWeek Day { get; set; }
        public TimeOnly Time { get; set; }
    }

    private class DayDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is DayOfWeek d ? Client.ShortDay(d) : string.Empty;
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}

file static class GridExtensions
{
    public static Grid WithGridChildren(this Grid grid, params object[] childrenWithColumns)
    {
        // pairs: (View, int col), (View, int col), ...
        grid.Children.Clear();
        for (int i = 0; i + 1 < childrenWithColumns.Length; i += 2)
        {
            var view = (View)childrenWithColumns[i];
            var col = (int)childrenWithColumns[i + 1];
            Grid.SetColumn(view, col);
            grid.Children.Add(view);
        }
        return grid;
    }
}
