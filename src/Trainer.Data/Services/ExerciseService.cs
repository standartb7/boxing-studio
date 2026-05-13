using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class ExerciseService : IExerciseService
{
    private readonly TrainerDbContext _db;

    public ExerciseService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<Exercise>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Exercises.AsNoTracking()
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Exercise>> GetByCategoryAsync(ExerciseCategory category, CancellationToken ct = default) =>
        await _db.Exercises.AsNoTracking()
            .Where(e => e.Category == category)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Exercise>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync(ct);

        var like = $"%{query.Trim()}%";
        return await _db.Exercises.AsNoTracking()
            .Where(e => EF.Functions.Like(e.Name, like)
                     || (e.Description != null && EF.Functions.Like(e.Description, like)))
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
    }

    public Task<Exercise?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Exercises.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Exercise> CreateAsync(Exercise exercise, CancellationToken ct = default)
    {
        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync(ct);
        return exercise;
    }

    public async Task UpdateAsync(Exercise exercise, CancellationToken ct = default)
    {
        _db.Exercises.Update(exercise);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.Exercises.FindAsync(new object[] { id }, ct);
        if (e is null) return;

        var inUse = await _db.TemplateItems.AnyAsync(t => t.ExerciseId == id, ct)
                 || await _db.WorkoutItems.AnyAsync(w => w.ExerciseId == id, ct);
        if (inUse)
            throw new InvalidOperationException("Упражнение используется в шаблонах или тренировках и не может быть удалено.");

        _db.Exercises.Remove(e);
        await _db.SaveChangesAsync(ct);
    }
}
