namespace Trainer.Core.Entities;

/// <summary>
/// Один слот расписания клиента: «Понедельник 18:00».
/// У клиента может быть много слотов — Пн 18:00 + Ср 20:00 + ...
/// </summary>
public class ScheduleSlot : EntityBase
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public DayOfWeek Day { get; set; }
    public TimeOnly Time { get; set; }
}
