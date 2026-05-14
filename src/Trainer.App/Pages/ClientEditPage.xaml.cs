using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Pages;

public partial class ClientEditPage : ContentPage
{
    private readonly IClientService _clients;
    private Client? _editing;

    // Чекбоксы для дней недели — заполняются динамически.
    private readonly Dictionary<WorkoutDays, CheckBox> _dayCheckboxes = new();

    public ClientEditPage(IClientService clients)
    {
        InitializeComponent();
        _clients = clients;

        BuildTypePicker();
        BuildDayCheckboxes();
    }

    /// <summary>
    /// Передаётся клиент для редактирования (null = создание).
    /// defaultType — если создаём из конкретной группы, чтобы Picker сразу был на ней.
    /// </summary>
    public void SetClient(Client? client, TrainingType? defaultType)
    {
        _editing = client;

        if (client is null)
        {
            Title = "Новый клиент";
            DeleteBtn.IsVisible = false;
            LastNameEntry.Text = string.Empty;
            FirstNameEntry.Text = string.Empty;
            PhoneEntry.Text = string.Empty;
            NotesEditor.Text = string.Empty;
            TypePicker.SelectedIndex = (int)(defaultType ?? TrainingType.Personal);
            foreach (var (_, cb) in _dayCheckboxes) cb.IsChecked = false;
            TimePickerControl.Time = new TimeSpan(19, 0, 0);
        }
        else
        {
            Title = client.FullName;
            DeleteBtn.IsVisible = true;
            LastNameEntry.Text = client.LastName;
            FirstNameEntry.Text = client.FirstName;
            PhoneEntry.Text = client.Phone;
            NotesEditor.Text = client.Notes;
            TypePicker.SelectedItem = client.TrainingType;
            foreach (var (day, cb) in _dayCheckboxes)
                cb.IsChecked = client.WorkoutDays.HasFlag(day);
            TimePickerControl.Time = client.WorkoutTime is null
                ? new TimeSpan(19, 0, 0)
                : new TimeSpan(client.WorkoutTime.Value.Hour, client.WorkoutTime.Value.Minute, 0);
        }
    }

    private void BuildTypePicker()
    {
        TypePicker.ItemsSource = Enum.GetValues<TrainingType>().Cast<object>().ToList();
        TypePicker.ItemDisplayBinding = new Binding(".", converter: new TrainingTypeDisplayConverter());
        TypePicker.SelectedIndex = 0;
    }

    private void BuildDayCheckboxes()
    {
        DaysPanel.Children.Clear();
        _dayCheckboxes.Clear();
        foreach (var (day, shortName) in WorkoutDaysExtensions.EnumerateAll())
        {
            var cb = new CheckBox();
            var label = new Label { Text = shortName, VerticalOptions = LayoutOptions.Center };
            var stack = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                Children = { cb, label }
            };
            _dayCheckboxes[day] = cb;
            DaysPanel.Children.Add(stack);
        }
    }

    private WorkoutDays CollectDays()
    {
        var result = WorkoutDays.None;
        foreach (var (day, cb) in _dayCheckboxes)
            if (cb.IsChecked) result |= day;
        return result;
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

        var selectedType = TypePicker.SelectedItem is TrainingType t ? t : TrainingType.Personal;
        var time = new TimeOnly(TimePickerControl.Time.Hours, TimePickerControl.Time.Minutes);

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
                    TrainingType = selectedType,
                    WorkoutDays = CollectDays(),
                    WorkoutTime = time,
                };
                await _clients.CreateAsync(client);
            }
            else
            {
                _editing.LastName = lastName;
                _editing.FirstName = firstName;
                _editing.Phone = PhoneEntry.Text?.Trim();
                _editing.Notes = NotesEditor.Text?.Trim();
                _editing.TrainingType = selectedType;
                _editing.WorkoutDays = CollectDays();
                _editing.WorkoutTime = time;
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

    private class TrainingTypeDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is TrainingType t ? t.DisplayName() : string.Empty;
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
