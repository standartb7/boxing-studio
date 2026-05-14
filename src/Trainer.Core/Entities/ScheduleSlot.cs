namespace Trainer.Core.Entities;

/// <summary>
/// Один слот расписания сессии: «Понедельник 18:00».
/// У сессии может быть много слотов — Пн 18:00 + Ср 20:00 + ...
/// </summary>
public class ScheduleSlot : EntityBase
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public DayOfWeek Day { get; set; }
    public TimeOnly Time { get; set; }
}
