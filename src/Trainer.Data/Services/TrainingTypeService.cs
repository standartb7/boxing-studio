using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class TrainingTypeService : ITrainingTypeService
{
    private readonly TrainerDbContext _db;

    public TrainingTypeService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<TrainingType>> GetAllAsync(CancellationToken ct = default) =>
        await _db.TrainingTypes.AsNoTracking()
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .ToListAsync(ct);

    public Task<TrainingType?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.TrainingTypes.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TrainingType> CreateOrGetAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Имя группы не может быть пустым", nameof(name));

        // SQLite collation NOCASE автоматически делает поиск регистронезависимым.
        var existing = await _db.TrainingTypes
            .FirstOrDefaultAsync(t => t.Name == trimmed, ct);
        if (existing is not null) return existing;

        var maxOrder = await _db.TrainingTypes.AnyAsync(ct)
            ? await _db.TrainingTypes.MaxAsync(t => (int?)t.SortOrder, ct) ?? 0
            : 0;

        var entity = new TrainingType { Name = trimmed, SortOrder = maxOrder + 1 };
        _db.TrainingTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        var trimmed = newName.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Имя не может быть пустым", nameof(newName));

        var entity = await _db.TrainingTypes.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException("Группа не найдена");

        var conflict = await _db.TrainingTypes
            .AnyAsync(t => t.Id != id && t.Name == trimmed, ct);
        if (conflict)
            throw new InvalidOperationException($"Группа «{trimmed}» уже существует");

        entity.Name = trimmed;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var hasClients = await _db.Clients.AnyAsync(c => c.TrainingTypeId == id, ct);
        if (hasClients)
            throw new InvalidOperationException("Нельзя удалить группу, в которой есть клиенты");

        var entity = await _db.TrainingTypes.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        _db.TrainingTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
