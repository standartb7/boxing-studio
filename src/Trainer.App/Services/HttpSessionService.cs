using Trainer.App.Api;
using Trainer.Contracts;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.App.Services;

public class HttpSessionService : ISessionService
{
    private readonly ISessionApi _api;
    private readonly IClientApi _clients;

    public HttpSessionService(ISessionApi api, IClientApi clients)
    {
        _api = api;
        _clients = clients;
    }

    public async Task<IReadOnlyList<Session>> GetByTypeAsync(Guid trainingTypeId, CancellationToken ct = default)
    {
        var dtos = await _api.GetAllAsync(trainingTypeId, ct);
        return (await HydrateMembersAsync(dtos, ct)).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByTypeAsync(CancellationToken ct = default)
    {
        var dtos = await _api.GetAllAsync(ct: ct);
        return dtos.GroupBy(s => s.TrainingTypeId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var dto = await _api.GetByIdAsync(id, ct);
            var hydrated = await HydrateMembersAsync(new[] { dto }, ct);
            return hydrated.FirstOrDefault();
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Session> CreateAsync(Session session, CancellationToken ct = default)
    {
        var req = new CreateSessionRequest
        {
            Title = session.Title,
            TrainingTypeId = session.TrainingTypeId,
            Notes = session.Notes,
            OwnerTrainerId = session.OwnerTrainerId == Guid.Empty ? null : session.OwnerTrainerId,
            Schedule = session.Schedule.Select(sl => new ScheduleSlotDto { Day = sl.Day, Time = sl.Time }).ToList(),
            MemberIds = session.Members.Select(m => m.Id).ToList(),
        };
        var dto = await _api.CreateAsync(req, ct);
        var hydrated = await HydrateMembersAsync(new[] { dto }, ct);
        return hydrated.First();
    }

    public async Task UpdateAsync(Session session, CancellationToken ct = default)
    {
        await _api.UpdateAsync(session.Id, new UpdateSessionRequest
        {
            Title = session.Title,
            TrainingTypeId = session.TrainingTypeId,
            Notes = session.Notes,
            IsActive = session.IsActive,
            Schedule = session.Schedule.Select(sl => new ScheduleSlotDto { Day = sl.Day, Time = sl.Time }).ToList(),
            MemberIds = session.Members.Select(m => m.Id).ToList(),
        }, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try { await _api.DeleteAsync(id, ct); }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }

    public async Task AddMemberAsync(Guid sessionId, Guid clientId, CancellationToken ct = default)
    {
        // No dedicated endpoint — fetch full session, add member, PUT.
        var dto = await _api.GetByIdAsync(sessionId, ct);
        if (dto.MemberIds.Contains(clientId)) return;
        var ids = dto.MemberIds.ToList();
        ids.Add(clientId);
        await _api.UpdateAsync(sessionId, new UpdateSessionRequest
        {
            Title = dto.Title,
            TrainingTypeId = dto.TrainingTypeId,
            Notes = dto.Notes,
            IsActive = dto.IsActive,
            Schedule = dto.Schedule,
            MemberIds = ids,
        }, ct);
    }

    public async Task RemoveMemberAsync(Guid sessionId, Guid clientId, CancellationToken ct = default)
    {
        var dto = await _api.GetByIdAsync(sessionId, ct);
        if (!dto.MemberIds.Contains(clientId)) return;
        var ids = dto.MemberIds.Where(i => i != clientId).ToList();
        await _api.UpdateAsync(sessionId, new UpdateSessionRequest
        {
            Title = dto.Title,
            TrainingTypeId = dto.TrainingTypeId,
            Notes = dto.Notes,
            IsActive = dto.IsActive,
            Schedule = dto.Schedule,
            MemberIds = ids,
        }, ct);
    }

    /// <summary>
    /// SessionDto only contains member ids. ViewModels expect full Client objects (Name etc.)
    /// in DisplayTitle and member lists, so we fetch the clients once and join in-memory.
    /// </summary>
    private async Task<IEnumerable<Session>> HydrateMembersAsync(
        IEnumerable<SessionDto> dtos, CancellationToken ct)
    {
        var sessions = dtos.Select(d => d.ToEntity()).ToList();
        var memberIds = sessions.SelectMany(s => s.Members.Select(m => m.Id)).Distinct().ToHashSet();
        if (memberIds.Count == 0) return sessions;

        var clients = await _clients.GetAllAsync(ct);
        var byId = clients.Where(c => memberIds.Contains(c.Id)).ToDictionary(c => c.Id, c => c.ToEntity());

        foreach (var s in sessions)
        {
            s.Members = s.Members
                .Select(m => byId.TryGetValue(m.Id, out var full) ? full : m)
                .ToList();
        }
        return sessions;
    }
}
