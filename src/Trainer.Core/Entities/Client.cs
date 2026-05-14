namespace Trainer.Core.Entities;

public class Client : EntityBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>На какой тип тренировок ходит клиент. Используется как «группа».</summary>
    public TrainingType TrainingType { get; set; } = TrainingType.Personal;

    /// <summary>В какие дни недели приходит. Можно несколько (Flags).</summary>
    public WorkoutDays WorkoutDays { get; set; } = WorkoutDays.None;

    /// <summary>Во сколько приходит. Если у клиента разное время в разные дни — пока не поддерживаем, договорились начать с одного.</summary>
    public TimeOnly? WorkoutTime { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{LastName} {FirstName}".Trim();

    /// <summary>«Пн, Ср, Пт · 19:00» — для отображения в списке.</summary>
    public string ScheduleDisplay
    {
        get
        {
            var days = WorkoutDays.FormatShort();
            if (WorkoutTime is null)
                return string.IsNullOrEmpty(days) ? string.Empty : days;
            var time = WorkoutTime.Value.ToString("HH:mm");
            return string.IsNullOrEmpty(days) ? time : $"{days} · {time}";
        }
    }
}
