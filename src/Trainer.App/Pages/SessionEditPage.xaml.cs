using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Pages;

public partial class SessionEditPage : ContentPage
{
    private readonly ISessionService _sessions;
    private readonly ITrainingTypeService _types;
    private readonly IClientService _clients;
    private readonly IServiceProvider _services;

    private Session? _editing;
    private Guid? _pendingDefaultTypeId;
    private bool _initialized;

    private List<TrainingType> _availableTypes = new();
    private readonly List<SlotRow> _slots = new();
    private readonly List<Client> _members = new();

    public SessionEditPage(ISessionService sessions, ITrainingTypeService types, IClientService clients, IServiceProvider services)
    {
        InitializeComponent();
        _sessions = sessions;
        _types = types;
        _clients = clients;
        _services = services;
    }

    public void SetSession(Session? session, Guid? defaultTypeId)
    {
        _editing = session;
        _pendingDefaultTypeId = defaultTypeId;
        _initialized = false; // новая «сессия для редактирования» → нужно заново инициализировать
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // OnAppearing вызывается каждый раз когда страница появляется (включая возврат
        // после закрытия модалки выбора участника). Инициализируем только при первом показе,
        // иначе затрём слоты/участников, добавленные пользователем в форму.
        if (_initialized) return;
        _initialized = true;

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
            Title = "Новая сессия";
            DeleteBtn.IsVisible = false;
            TitleEntry.Text = string.Empty;
            NotesEditor.Text = string.Empty;

            var defaultType = _availableTypes.FirstOrDefault(t => t.Id == _pendingDefaultTypeId)
                ?? _availableTypes.FirstOrDefault();
            TypePicker.SelectedItem = defaultType;

            _slots.Clear();
            _members.Clear();
        }
        else
        {
            // Перезагружаем сессию — slots/members могут отличаться от того, что лежало в карточке списка.
            var fresh = await _sessions.GetByIdAsync(_editing.Id);
            if (fresh is not null) _editing = fresh;

            Title = _editing.DisplayTitle;
            DeleteBtn.IsVisible = true;
            TitleEntry.Text = _editing.Title;
            NotesEditor.Text = _editing.Notes;
            TypePicker.SelectedItem = _availableTypes.FirstOrDefault(t => t.Id == _editing.TrainingTypeId);

            _slots.Clear();
            foreach (var s in _editing.Schedule.OrderBy(s => Client.DayOrder(s.Day)).ThenBy(s => s.Time))
                _slots.Add(new SlotRow { Day = s.Day, Time = s.Time });

            _members.Clear();
            _members.AddRange(_editing.Members.OrderBy(m => m.Name));
        }

        RebuildSlotsPanel();
        RebuildMembersPanel();
    }

    // ---- слоты ----

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
                Text = "Нет слотов. Нажми «+ День».",
                FontSize = 13,
                TextColor = Colors.Gray,
            });
            return;
        }

        foreach (var slot in _slots)
            SlotsPanel.Children.Add(BuildSlotRow(slot));
    }

    private View BuildSlotRow(SlotRow slot)
    {
        var dayPicker = new Picker
        {
            ItemsSource = Enum.GetValues<DayOfWeek>().OrderBy(Client.DayOrder).Cast<object>().ToList(),
            SelectedItem = slot.Day,
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

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            },
            ColumnSpacing = 8,
        };
        Grid.SetColumn(dayPicker, 0);
        Grid.SetColumn(timePicker, 1);
        Grid.SetColumn(removeBtn, 2);
        grid.Children.Add(dayPicker);
        grid.Children.Add(timePicker);
        grid.Children.Add(removeBtn);
        return grid;
    }

    // ---- участники ----

    private async void OnAddMemberClicked(object sender, EventArgs e)
    {
        // Открываем модалку выбора с поиском. Создание нового — кнопка «+ Новый» в toolbar.
        var picker = _services.GetRequiredService<ClientPickerPage>();
        picker.SetExcluded(_members.Select(m => m.Id));
        picker.Picked = client =>
        {
            _members.Add(client);
            RebuildMembersPanel();
        };

        // Заворачиваем в NavigationPage чтобы кастомный header отображался корректно.
        await Navigation.PushModalAsync(new NavigationPage(picker));
    }

    private async Task<Client?> PromptCreateClientAsync()
    {
        var name = await DisplayPromptAsync("Новый участник", "Имя:", "Далее", "Отмена", placeholder: "Иван Петров");
        if (name is null) return null; // отмена

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ошибка", "Имя не может быть пустым", "OK");
            return null;
        }

        var phone = await DisplayPromptAsync("Новый участник", "Телефон (необязательно):", "Создать", "Отмена",
            placeholder: "+373 60 123 456", keyboard: Keyboard.Telephone);
        // phone == null означает отмену → создавать не будем
        if (phone is null) return null;

        var client = new Client
        {
            Name = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        };

        return await _clients.CreateAsync(client);
    }

    private async Task<bool> PromptEditClientAsync(Client client)
    {
        var name = await DisplayPromptAsync("Редактировать", "Имя:", "Далее", "Отмена",
            initialValue: client.Name, placeholder: "Иван Петров");
        if (name is null) return false;

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ошибка", "Имя не может быть пустым", "OK");
            return false;
        }

        var phone = await DisplayPromptAsync("Редактировать", "Телефон:", "Сохранить", "Отмена",
            initialValue: client.Phone ?? string.Empty, placeholder: "+373 60 123 456", keyboard: Keyboard.Telephone);
        if (phone is null) return false;

        client.Name = name.Trim();
        client.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        try
        {
            await _clients.UpdateAsync(client);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
            return false;
        }
        return true;
    }

    private void RebuildMembersPanel()
    {
        MembersPanel.Children.Clear();
        if (_members.Count == 0)
        {
            MembersPanel.Children.Add(new Label
            {
                Text = "Нет участников. Нажми «+ Участник».",
                FontSize = 13,
                TextColor = Colors.Gray,
            });
            return;
        }

        foreach (var m in _members)
            MembersPanel.Children.Add(BuildMemberRow(m));
    }

    private View BuildMemberRow(Client client)
    {
        var nameLabel = new Label
        {
            Text = client.FullName,
            FontSize = 15,
            VerticalOptions = LayoutOptions.Center,
        };
        var phoneLabel = new Label
        {
            Text = client.Phone ?? string.Empty,
            FontSize = 12,
            TextColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center,
        };
        var callBtn = new Button
        {
            Text = "📞",
            FontSize = 18,
            Padding = new Thickness(10, 4),
            MinimumHeightRequest = 32,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            IsVisible = !string.IsNullOrWhiteSpace(client.Phone),
        };
        callBtn.Clicked += (_, _) => TryDial(client.Phone);

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
            _members.Remove(client);
            RebuildMembersPanel();
        };

        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Children.Add(nameLabel);
        if (!string.IsNullOrWhiteSpace(client.Phone))
            stack.Children.Add(phoneLabel);

        // Тап по имени/телефону → редактирование клиента (имя/фамилия/телефон).
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            var ok = await PromptEditClientAsync(client);
            if (ok)
            {
                nameLabel.Text = client.FullName;
                phoneLabel.Text = client.Phone ?? string.Empty;
                callBtn.IsVisible = !string.IsNullOrWhiteSpace(client.Phone);
                // если телефон появился/исчез — пересоберём строку, чтобы лейбл показывался/прятался
                RebuildMembersPanel();
            }
        };
        stack.GestureRecognizers.Add(tap);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        Grid.SetColumn(stack, 0);
        Grid.SetColumn(callBtn, 1);
        Grid.SetColumn(removeBtn, 2);
        grid.Children.Add(stack);
        grid.Children.Add(callBtn);
        grid.Children.Add(removeBtn);
        return grid;
    }

    private async void TryDial(string? phone)
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

    // ---- сохранение ----

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Title опционален: если пусто и в сессии 1 участник — DisplayTitle подставит его имя.
        var title = TitleEntry.Text?.Trim() ?? string.Empty;

        if (TypePicker.SelectedItem is not TrainingType type)
        {
            await DisplayAlert("Ошибка", "Выбери группу", "OK");
            return;
        }

        try
        {
            if (_editing is null)
            {
                var session = new Session
                {
                    Title = title,
                    TrainingTypeId = type.Id,
                    Notes = NotesEditor.Text?.Trim(),
                    Schedule = _slots.Select(s => new ScheduleSlot { Day = s.Day, Time = s.Time }).ToList(),
                };
                var created = await _sessions.CreateAsync(session);

                // Members добавляем после создания сессии — по AddMemberAsync чтобы не путаться с навигацией EF.
                foreach (var m in _members)
                    await _sessions.AddMemberAsync(created.Id, m.Id);
            }
            else
            {
                _editing.Title = title;
                _editing.TrainingTypeId = type.Id;
                _editing.Notes = NotesEditor.Text?.Trim();
                _editing.Schedule = _slots.Select(s => new ScheduleSlot { Day = s.Day, Time = s.Time }).ToList();
                _editing.Members = _members.ToList();
                await _sessions.UpdateAsync(_editing);
            }

            await Navigation.PopAsync();
        }
        catch (Refit.ApiException apiEx)
        {
            await DisplayAlert("Ошибка сохранения",
                $"HTTP {(int)apiEx.StatusCode}\n\n{apiEx.Content}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка сохранения", ex.Message, "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_editing is null) return;
        var confirm = await DisplayAlert("Удалить сессию?", $"Удалить «{_editing.DisplayTitle}»?", "Удалить", "Отмена");
        if (!confirm) return;

        try
        {
            await _sessions.DeleteAsync(_editing.Id);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    // ---- helpers ----

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
