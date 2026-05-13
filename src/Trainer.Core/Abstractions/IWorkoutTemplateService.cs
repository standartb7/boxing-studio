using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface IWorkoutTemplateService
{
    Task<IReadOnlyList<WorkoutTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<WorkoutTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WorkoutTemplate> CreateAsync(WorkoutTemplate template, CancellationToken ct = default);
    Task UpdateAsync(WorkoutTemplate template, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<WorkoutTemplate> DuplicateAsync(Guid templateId, string? newName = null, CancellationToken ct = default);

    Task AddItemAsync(Guid templateId, TemplateItem item, CancellationToken ct = default);
    Task UpdateItemAsync(TemplateItem item, CancellationToken ct = default);
    Task RemoveItemAsync(Guid itemId, CancellationToken ct = default);
    Task ReorderItemsAsync(Guid templateId, IReadOnlyList<Guid> orderedItemIds, CancellationToken ct = default);
}
