namespace Trainer.Core.Entities;

/// <summary>
/// Дни недели, в которые клиент посещает тренировки. Flags — можно комбинировать.
/// </summary>
[Flags]
public enum WorkoutDays
{
    None       = 0,
    Monday     = 1 << 0,
    Tuesday    = 1 << 1,
    Wednesday  = 1 << 2,
    Thursday   = 1 << 3,
    Friday     = 1 << 4,
    Saturday   = 1 << 5,
    Sunday     = 1 << 6,
}

public static class WorkoutDaysExtensions
{
    private static readonly (WorkoutDays Day, string Short)[] AllDays =
    {
        (WorkoutDays.Monday,    "Пн"),
        (WorkoutDays.Tuesday,   "Вт"),
        (WorkoutDays.Wednesday, "Ср"),
        (WorkoutDays.Thursday,  "Чт"),
        (WorkoutDays.Friday,    "Пт"),
        (WorkoutDays.Saturday,  "Сб"),
        (WorkoutDays.Sunday,    "Вс"),
    };

    /// <summary>Короткое представление: "Пн, Ср, Сб" или пусто если None.</summary>
    public static string FormatShort(this WorkoutDays days)
    {
        if (days == WorkoutDays.None) return string.Empty;
        var parts = AllDays.Where(d => days.HasFlag(d.Day)).Select(d => d.Short);
        return string.Join(", ", parts);
    }

    public static IEnumerable<(WorkoutDays Day, string Short)> EnumerateAll() => AllDays;
}
