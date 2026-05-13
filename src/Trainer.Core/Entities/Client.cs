namespace Trainer.Core.Entities;

public class Client : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoPath { get; set; }
    public DateOnly? StartDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ScheduledWorkout> Workouts { get; set; } = new();
}
