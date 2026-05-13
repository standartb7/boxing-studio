namespace Trainer.Core.Entities;

public class WorkoutTemplate : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public List<TemplateItem> Items { get; set; } = new();
}

public class TemplateItem : EntityBase
{
    public Guid TemplateId { get; set; }
    public WorkoutTemplate Template { get; set; } = null!;

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }

    // Раундовая работа (мешок, лапы, тень, скакалка, спарринг)
    public int? Rounds { get; set; }
    public int? RoundDurationSec { get; set; }
    public int? RestBetweenRoundsSec { get; set; }

    // Подходовая работа (ОФП — берпи, отжимания, штанга)
    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public double? Weight { get; set; }

    // Цельный временной блок (разминка / растяжка)
    public int? DurationSec { get; set; }

    // Боксёрская специфика
    public string? Combo { get; set; }       // "1-2, 1-2-3-2"
    public Intensity Intensity { get; set; } = Intensity.Medium;
    public string? Notes { get; set; }       // указания по технике
}
