using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface ITrainingTypeService
{
    Task<IReadOnlyList<TrainingType>> GetAllAsync(CancellationToken ct = default);
    Task<TrainingType?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создаёт новый тип. Возвращает существующий, если такой по имени уже есть (case-insensitive).
    /// </summary>
    Task<TrainingType> CreateOrGetAsync(string name, CancellationToken ct = default);

    /// <summary>Переименовать. Бросает InvalidOperationException если новое имя уже занято другим типом.</summary>
    Task RenameAsync(Guid id, string newName, CancellationToken ct = default);

    /// <summary>Удалить тип. Бросает InvalidOperationException если на тип ссылаются клиенты.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
