using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class ClientService(TrainerDbContext db) : IClientService
{
    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default) =>
        await db.Clients.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        client.Name = client.Name.Trim();
        await EnsureNameUniqueAsync(client.Name, excludeId: null, ct);

        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client;
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        client.Name = client.Name.Trim();
        await EnsureNameUniqueAsync(client.Name, excludeId: client.Id, ct);

        db.Clients.Update(client);
        await db.SaveChangesAsync(ct);
    }

    // NOCASE-коллация на колонке Name → сравнение в WHERE case-insensitive.
    private async Task EnsureNameUniqueAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var conflict = await db.Clients
            .AsNoTracking()
            .Where(c => c.Name == name && (excludeId == null || c.Id != excludeId))
            .FirstOrDefaultAsync(ct);
        if (conflict is not null)
            throw new InvalidOperationException($"Клиент с именем «{name}» уже существует.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Clients.FindAsync(new object[] { id }, ct);
        if (c is null) return;
        db.Clients.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSessionCountsAsync(CancellationToken ct = default)
    {
        var raw = await db.SessionMembers.AsNoTracking()
            .GroupBy(m => m.ClientId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return raw.ToDictionary(x => x.Id, x => x.Count);
    }
}
