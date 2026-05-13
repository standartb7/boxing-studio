using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class ClientService : IClientService
{
    private readonly TrainerDbContext _db;

    public ClientService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<Client>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.Clients.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);
        return client;
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Update(client);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Clients.FindAsync(new object[] { id }, ct);
        if (c is null) return;
        c.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
