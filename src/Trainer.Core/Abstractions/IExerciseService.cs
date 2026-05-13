using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface IExerciseService
{
    Task<IReadOnlyList<Exercise>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Exercise>> GetByCategoryAsync(ExerciseCategory category, CancellationToken ct = default);
    Task<IReadOnlyList<Exercise>> SearchAsync(string query, CancellationToken ct = default);
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Exercise> CreateAsync(Exercise exercise, CancellationToken ct = default);
    Task UpdateAsync(Exercise exercise, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
