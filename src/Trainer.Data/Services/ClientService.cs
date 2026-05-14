using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class ClientService : IClientService
{
    private readonly TrainerDbContext _db;

    public ClientService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Clients.AsNoTracking()
            .Include(c => c.Schedule)
            .Include(c => c.TrainingType)
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Client>> GetByTypeAsync(Guid trainingTypeId, CancellationToken ct = default) =>
        await _db.Clients.AsNoTracking()
            .Include(c => c.Schedule)
            .Where(c => c.IsActive && c.TrainingTypeId == trainingTypeId)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByTypeAsync(CancellationToken ct = default)
    {
        var raw = await _db.Clients.AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.TrainingTypeId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return raw.ToDictionary(x => x.Id, x => x.Count);
    }

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Clients
            .Include(c => c.Schedule)
            .Include(c => c.TrainingType)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);
        return client;
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        // Schedule полностью заменяется: удаляем старое, сохраняем новое.
        var existingSlots = _db.ScheduleSlots.Where(s => s.ClientId == client.Id);
        _db.ScheduleSlots.RemoveRange(existingSlots);

        _db.Clients.Update(client);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Clients.FindAsync(new object[] { id }, ct);
        if (c is null) return;
        _db.Clients.Remove(c);
        await _db.SaveChangesAsync(ct);
    }
}
