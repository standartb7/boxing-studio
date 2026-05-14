using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Client> CreateAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Сколько сессий у каждого клиента — для счётчика в списке клиентов.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetSessionCountsAsync(CancellationToken ct = default);
}
