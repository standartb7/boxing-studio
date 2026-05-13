using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetActiveAsync(CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Client> CreateAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task ArchiveAsync(Guid id, CancellationToken ct = default);
}
