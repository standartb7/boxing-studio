namespace Trainer.Core.Entities;

public enum WorkoutStatus
{
    Planned = 0,
    Done,
    Skipped,
    Cancelled,
}

public class ScheduledWorkout : EntityBase
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public DateTimeOffset StartAt { get; set; }
    public int DurationMin { get; set; } = 60;

    public Guid? TemplateId { get; set; }
    public WorkoutTemplate? Template { get; set; }

    public WorkoutStatus Status { get; set; } = WorkoutStatus.Planned;
    public string? Notes { get; set; }

    public List<WorkoutItem> Items { get; set; } = new();
}

public class WorkoutItem : EntityBase
{
    public Guid ScheduledWorkoutId { get; set; }
    public ScheduledWorkout ScheduledWorkout { get; set; } = null!;

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }

    public int? Rounds { get; set; }
    public int? RoundDurationSec { get; set; }
    public int? RestBetweenRoundsSec { get; set; }

    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public double? Weight { get; set; }

    public int? DurationSec { get; set; }

    public string? Combo { get; set; }
    public Intensity Intensity { get; set; } = Intensity.Medium;
    public string? Notes { get; set; }

    public bool Done { get; set; }
}
