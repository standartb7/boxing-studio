namespace Trainer.Core.Entities;

/// <summary>
/// Тип тренировки, на которую ходит клиент. Используется как «группа» в UI.
/// Тренер не может добавлять свои — изменения через релиз приложения.
/// </summary>
public enum TrainingType
{
    Personal = 0,    // персональные
    Group = 1,       // групповые
    Kids = 2,        // детские
    Other = 99,      // прочее
}

public static class TrainingTypeExtensions
{
    public static string DisplayName(this TrainingType type) => type switch
    {
        TrainingType.Personal => "Персональные",
        TrainingType.Group => "Групповые",
        TrainingType.Kids => "Детские",
        TrainingType.Other => "Другое",
        _ => type.ToString(),
    };
}
