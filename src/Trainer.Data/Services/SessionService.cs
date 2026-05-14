using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class SessionService : ISessionService
{
    private readonly TrainerDbContext _db;

    public SessionService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<Session>> GetByTypeAsync(Guid trainingTypeId, CancellationToken ct = default) =>
        await _db.Sessions.AsNoTracking()
            .Include(s => s.Schedule)
            .Include(s => s.Members)
            .Where(s => s.IsActive && s.TrainingTypeId == trainingTypeId)
            .OrderBy(s => s.Title)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByTypeAsync(CancellationToken ct = default)
    {
        var raw = await _db.Sessions.AsNoTracking()
            .Where(s => s.IsActive)
            .GroupBy(s => s.TrainingTypeId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return raw.ToDictionary(x => x.Id, x => x.Count);
    }

    public Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Sessions
            .Include(s => s.Schedule)
            .Include(s => s.Members)
            .Include(s => s.TrainingType)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Session> CreateAsync(Session session, CancellationToken ct = default)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateAsync(Session session, CancellationToken ct = default)
    {
        // Слоты заменяем целиком: удалить старые → вставить новые из переданной сессии.
        var existingSlots = _db.ScheduleSlots.Where(s => s.SessionId == session.Id);
        _db.ScheduleSlots.RemoveRange(existingSlots);

        // Members. Загружаем актуальный список из БД через join-таблицу,
        // считаем diff с переданным session.Members и обновляем SessionMembers соответственно.
        var existingMembers = await _db.SessionMembers
            .Where(m => m.SessionId == session.Id)
            .ToListAsync(ct);

        var newMemberIds = session.Members.Select(m => m.Id).ToHashSet();
        var existingMemberIds = existingMembers.Select(m => m.ClientId).ToHashSet();

        var toRemove = existingMembers.Where(m => !newMemberIds.Contains(m.ClientId));
        _db.SessionMembers.RemoveRange(toRemove);

        foreach (var clientId in newMemberIds.Except(existingMemberIds))
        {
            _db.SessionMembers.Add(new SessionMember { SessionId = session.Id, ClientId = clientId });
        }

        // Обновляем скалярные поля и slots через стандартный Update.
        // Members не трогаем напрямую — мы уже синхронизировали через SessionMember.
        var tracked = await _db.Sessions
            .Include(s => s.Schedule)
            .FirstOrDefaultAsync(s => s.Id == session.Id, ct);
        if (tracked is null) throw new InvalidOperationException("Сессия не найдена");

        tracked.Title = session.Title;
        tracked.TrainingTypeId = session.TrainingTypeId;
        tracked.Notes = session.Notes;
        tracked.IsActive = session.IsActive;

        // Старые slots уже помечены к удалению выше; добавляем новые с привязкой к session.
        foreach (var slot in session.Schedule)
        {
            slot.SessionId = session.Id;
            _db.ScheduleSlots.Add(slot);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.Sessions.FindAsync(new object[] { id }, ct);
        if (s is null) return;
        _db.Sessions.Remove(s);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddMemberAsync(Guid sessionId, Guid clientId, CancellationToken ct = default)
    {
        var exists = await _db.SessionMembers
            .AnyAsync(m => m.SessionId == sessionId && m.ClientId == clientId, ct);
        if (exists) return;

        _db.SessionMembers.Add(new SessionMember { SessionId = sessionId, ClientId = clientId });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid sessionId, Guid clientId, CancellationToken ct = default)
    {
        var member = await _db.SessionMembers
            .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.ClientId == clientId, ct);
        if (member is null) return;

        _db.SessionMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
    }
}
