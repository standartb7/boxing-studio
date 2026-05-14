namespace Trainer.Core.Entities;

/// <summary>
/// Тренировка — единица расписания. Имеет название, тип, расписание (слоты по дням),
/// и список участников (клиентов). Для персональной тренировки — 1 участник, для групповой — N.
/// </summary>
public class Session : EntityBase
{
    public string Title { get; set; } = string.Empty;

    public Guid TrainingTypeId { get; set; }
    public TrainingType TrainingType { get; set; } = null!;

    public List<ScheduleSlot> Schedule { get; set; } = new();
    public List<Client> Members { get; set; } = new();

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Заголовок для UI: явный Title если задан, иначе имя единственного участника,
    /// иначе fallback. Тренер не должен придумывать названия для личных тренировок.
    /// </summary>
    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title)) return Title;
            if (Members.Count == 1) return Members[0].FullName;
            return Members.Count == 0 ? "Без названия" : $"Группа ({Members.Count})";
        }
    }

    /// <summary>«Пн 18:00, Ср 20:00, Сб 10:00» — для отображения в списке.</summary>
    public string ScheduleDisplay
    {
        get
        {
            if (Schedule is null || Schedule.Count == 0) return string.Empty;

            var ordered = Schedule
                .OrderBy(s => Client.DayOrder(s.Day))
                .ThenBy(s => s.Time)
                .Select(s => $"{Client.ShortDay(s.Day)} {s.Time:HH\\:mm}");

            return string.Join(", ", ordered);
        }
    }
}
