namespace Trainer.Core.Entities;

public class Client : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid OwnerTrainerId { get; set; }

    public List<Session> Sessions { get; set; } = new();

    // Алиас для совместимости с UI-биндингами, которые могут использовать FullName.
    public string FullName => Name;

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
