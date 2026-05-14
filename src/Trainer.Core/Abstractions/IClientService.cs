using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Client>> GetByTypeAsync(Guid trainingTypeId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> GetCountsByTypeAsync(CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Client> CreateAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
