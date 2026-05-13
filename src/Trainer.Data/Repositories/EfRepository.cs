using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Repositories;

public class EfRepository<T> : IRepository<T> where T : EntityBase
{
    private readonly TrainerDbContext _db;
    private readonly DbSet<T> _set;

    public EfRepository(TrainerDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await _set.AsNoTracking().ToListAsync(ct);

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _set.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _set.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
