namespace Trainer.Core.Entities;

public class Client : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid OwnerTrainerId { get; set; }

    /// <summary>
    /// Display name of the owning trainer — populated by the mobile mapper from
    /// ClientDto. Not persisted in the database (server keeps the join out of EF).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? OwnerDisplayName { get; set; }

    /// <summary>Set by the mobile UI when the current user is a HeadTrainer — drives the reassign swipe action visibility.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool CanReassign { get; set; }

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
