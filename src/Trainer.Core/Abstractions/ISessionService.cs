using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface ISessionService
{
    Task<IReadOnlyList<Session>> GetByTypeAsync(Guid trainingTypeId, CancellationToken ct = default);

    /// <summary>Сколько сессий в каждом типе — для бейджей на GroupsPage.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetCountsByTypeAsync(CancellationToken ct = default);

    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Session> CreateAsync(Session session, CancellationToken ct = default);

    /// <summary>Заменяет slots и members сессии (delete-then-insert).</summary>
    Task UpdateAsync(Session session, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task AddMemberAsync(Guid sessionId, Guid clientId, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid sessionId, Guid clientId, CancellationToken ct = default);
}
