namespace Trainer.Core.Entities;

/// <summary>
/// Тип тренировки — «группа» в UI. Тренер сам создаёт/именует.
/// Уникальность по имени (case-insensitive) обеспечивается на уровне сервиса.
/// </summary>
public class TrainingType : EntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Порядок отображения в списке. Меньше — выше.</summary>
    public int SortOrder { get; set; }
}
