namespace Trainer.Core.Entities;

/// <summary>
/// Связка «клиент → сессия». Явная join-таблица: даёт нам гибкость на будущее
/// (можно будет добавить JoinedAt, IsActive в этой группе, и т.п.).
/// </summary>
public class SessionMember : EntityBase
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
}
