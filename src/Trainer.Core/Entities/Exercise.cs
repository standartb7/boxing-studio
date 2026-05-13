namespace Trainer.Core.Entities;

public enum ExerciseCategory
{
    Other = 0,
    WarmUp,        // разминка
    JumpRope,      // скакалка
    Shadow,        // бой с тенью
    Bag,           // мешок
    Mitts,         // лапы
    Pneumo,        // пневмогруша / груша на резинках
    Sparring,      // спарринг
    Technique,     // отработка одиночных, передвижений
    Conditioning,  // ОФП
    Plyometrics,   // СФП / взрывная
    Cardio,        // бег, велотренажёр
    Stretching,    // растяжка / заминка
}

public enum Intensity
{
    Light = 0,
    Medium,
    Hard,
}

public class Exercise : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public ExerciseCategory Category { get; set; } = ExerciseCategory.Other;
    public string? Description { get; set; }
    public string? MediaPath { get; set; }
}
