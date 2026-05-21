using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Api.Mapping;
using Trainer.Contracts;
using Trainer.Core.Entities;
using Trainer.Infrastructure;

namespace Trainer.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly TrainerDbContext _db;
    private readonly ITrainerScope _scope;

    public SessionsController(TrainerDbContext db, ITrainerScope scope)
    {
        _db = db;
        _scope = scope;
    }

    private IQueryable<Session> Scoped() =>
        _scope.IsHeadTrainer
            ? _db.Sessions
            : _db.Sessions.Where(s => s.OwnerTrainerId == _scope.UserId);

    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> GetAll(
        [FromQuery] Guid? trainingTypeId,
        [FromQuery] Guid? ownerTrainerId,
        CancellationToken ct)
    {
        var q = Scoped().AsNoTracking()
            .Include(s => s.Schedule)
            .Include(s => s.Members)
            .Where(s => s.IsActive);
        if (trainingTypeId is { } tid) q = q.Where(s => s.TrainingTypeId == tid);
        if (ownerTrainerId is { } owner)
        {
            if (!_scope.IsHeadTrainer && owner != _scope.UserId) return Forbid();
            q = q.Where(s => s.OwnerTrainerId == owner);
        }

        // LEFT JOIN on User so each session row carries the trainer's display name.
        // Ordering: by trainer name (so all sessions of one trainer cluster together),
        // then by title within the trainer.
        var rows = await (
            from s in q
            join u in _db.Users on s.OwnerTrainerId equals u.Id into us
            from u in us.DefaultIfEmpty()
            orderby u != null ? u.DisplayName : string.Empty, s.Title
            select new { Session = s, OwnerName = u != null ? u.DisplayName : null }
        ).ToListAsync(ct);

        return Ok(rows.Select(r => r.Session.ToDto(r.OwnerName)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDto>> GetById(Guid id, CancellationToken ct)
    {
        var row = await (
            from s in Scoped().AsNoTracking()
                .Include(x => x.Schedule)
                .Include(x => x.Members)
            where s.Id == id
            join u in _db.Users on s.OwnerTrainerId equals u.Id into us
            from u in us.DefaultIfEmpty()
            select new { Session = s, OwnerName = u != null ? u.DisplayName : null }
        ).FirstOrDefaultAsync(ct);

        return row is null ? NotFound() : Ok(row.Session.ToDto(row.OwnerName));
    }

    [HttpPost]
    public async Task<ActionResult<SessionDto>> Create(CreateSessionRequest req, CancellationToken ct)
    {
        // Title can be blank — clients render a fallback from member names. Persisted as-is.
        if (!await _db.TrainingTypes.AnyAsync(t => t.Id == req.TrainingTypeId, ct))
            return BadRequest(new { error = "Unknown trainingTypeId." });

        var ownerId = _scope.IsHeadTrainer && req.OwnerTrainerId is { } v ? v : _scope.UserId;
        if (_scope.IsHeadTrainer && req.OwnerTrainerId is { } v2
            && !await _db.Users.AnyAsync(u => u.Id == v2 && u.IsActive, ct))
        {
            return BadRequest(new { error = "Owner trainer not found or inactive." });
        }

        var session = new Session
        {
            Title = req.Title?.Trim() ?? string.Empty,
            TrainingTypeId = req.TrainingTypeId,
            Notes = req.Notes,
            IsActive = true,
            OwnerTrainerId = ownerId,
            Schedule = req.Schedule.Select(sl => new ScheduleSlot { Day = sl.Day, Time = sl.Time }).ToList(),
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);

        foreach (var memberId in req.MemberIds.Distinct())
        {
            _db.SessionMembers.Add(new SessionMember { SessionId = session.Id, ClientId = memberId });
        }
        if (req.MemberIds.Count > 0) await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = session.Id }, await ReloadDto(session.Id, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SessionDto>> Update(Guid id, UpdateSessionRequest req, CancellationToken ct)
    {
        var session = await Scoped()
            .Include(s => s.Schedule)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();

        if (!await _db.TrainingTypes.AnyAsync(t => t.Id == req.TrainingTypeId, ct))
            return BadRequest(new { error = "Unknown trainingTypeId." });

        session.Title = req.Title?.Trim() ?? string.Empty;
        session.TrainingTypeId = req.TrainingTypeId;
        session.Notes = req.Notes;
        session.IsActive = req.IsActive;

        _db.ScheduleSlots.RemoveRange(session.Schedule);
        foreach (var sl in req.Schedule)
            _db.ScheduleSlots.Add(new ScheduleSlot { SessionId = id, Day = sl.Day, Time = sl.Time });

        var existingMembers = await _db.SessionMembers.Where(m => m.SessionId == id).ToListAsync(ct);
        var newSet = req.MemberIds.ToHashSet();
        var existingSet = existingMembers.Select(m => m.ClientId).ToHashSet();
        _db.SessionMembers.RemoveRange(existingMembers.Where(m => !newSet.Contains(m.ClientId)));
        foreach (var clientId in newSet.Except(existingSet))
            _db.SessionMembers.Add(new SessionMember { SessionId = id, ClientId = clientId });

        await _db.SaveChangesAsync(ct);
        return Ok(await ReloadDto(id, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var session = await Scoped().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();

        _db.Sessions.Remove(session); // cascade drops SessionMember/ScheduleSlot
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<SessionDto> ReloadDto(Guid id, CancellationToken ct)
    {
        var row = await (
            from s in _db.Sessions.AsNoTracking()
                .Include(x => x.Schedule)
                .Include(x => x.Members)
            where s.Id == id
            join u in _db.Users on s.OwnerTrainerId equals u.Id into us
            from u in us.DefaultIfEmpty()
            select new { Session = s, OwnerName = u != null ? u.DisplayName : null }
        ).FirstAsync(ct);
        return row.Session.ToDto(row.OwnerName);
    }
}
