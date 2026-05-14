namespace Trainer.Core.Entities;

public class Client : EntityBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public Guid TrainingTypeId { get; set; }
    public TrainingType TrainingType { get; set; } = null!;

    public List<ScheduleSlot> Schedule { get; set; } = new();

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{LastName} {FirstName}".Trim();

    /// <summary>
    /// «Пн 18:00, Ср 20:00, Сб 10:00» — для строки в списке клиентов.
    /// Слоты группируются по дню и сортируются Пн→Вс.
    /// </summary>
    public string ScheduleDisplay
    {
        get
        {
            if (Schedule is null || Schedule.Count == 0) return string.Empty;

            var ordered = Schedule
                .OrderBy(s => DayOrder(s.Day))
                .ThenBy(s => s.Time)
                .Select(s => $"{ShortDay(s.Day)} {s.Time:HH\\:mm}");

            return string.Join(", ", ordered);
        }
    }

    public static string ShortDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Пн",
        DayOfWeek.Tuesday => "Вт",
        DayOfWeek.Wednesday => "Ср",
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        DayOfWeek.Sunday => "Вс",
        _ => day.ToString(),
    };

    // В .NET воскресенье = 0; для UX перекладываем: Пн=0, Вс=6.
    public static int DayOrder(DayOfWeek day) => ((int)day + 6) % 7;
}
