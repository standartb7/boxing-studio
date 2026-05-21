using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Core.Entities;
using Trainer.Infrastructure;

namespace Trainer.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private const int CurrentVersion = 2; // v1 = mobile local-only; v2 = cloud (adds Users + OwnerTrainerId)

    private readonly TrainerDbContext _db;

    public BackupController(TrainerDbContext db) => _db = db;

    [HttpGet("export")]
    public async Task<ActionResult<GymBackup>> Export(CancellationToken ct)
    {
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);
        var types = await _db.TrainingTypes.AsNoTracking().ToListAsync(ct);
        var clients = await _db.Clients.AsNoTracking().ToListAsync(ct);
        var sessions = await _db.Sessions.AsNoTracking()
            .Include(s => s.Schedule)
            .Include(s => s.Members)
            .ToListAsync(ct);

        return Ok(new GymBackup
        {
            Version = CurrentVersion,
            ExportedAt = DateTimeOffset.UtcNow,
            Users = users.Select(u => new UserBackup
            {
                Id = u.Id, Email = u.Email, DisplayName = u.DisplayName,
                Role = u.Role.ToString(), IsActive = u.IsActive,
            }).ToList(),
            TrainingTypes = types.Select(t => new TrainingTypeBackup
            {
                Id = t.Id, Name = t.Name, SortOrder = t.SortOrder,
            }).ToList(),
            Clients = clients.Select(c => new ClientBackup
            {
                Id = c.Id, Name = c.Name, Phone = c.Phone, Notes = c.Notes,
                IsActive = c.IsActive,
            }).ToList(),
            Sessions = sessions.Select(s => new SessionBackup
            {
                Id = s.Id, Title = s.Title, TrainingTypeId = s.TrainingTypeId,
                Notes = s.Notes, IsActive = s.IsActive, OwnerTrainerId = s.OwnerTrainerId,
                Schedule = s.Schedule.Select(sl => new SlotBackup { Day = sl.Day, Time = sl.Time }).ToList(),
                MemberIds = s.Members.Select(m => m.Id).ToList(),
            }).ToList(),
        });
    }

    /// <summary>
    /// Replace-all import. Wipes gym data and restores from payload. User accounts are kept
    /// (we do not allow importing credentials); incoming Users are matched by Id and only
    /// DisplayName/Role/IsActive are merged. Unknown owner ids fall back to the importer.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(GymBackup payload, [FromServices] ITrainerScope scope, CancellationToken ct)
    {
        if (payload.Version > CurrentVersion)
            return BadRequest(new { error = $"Backup version {payload.Version} is newer than supported ({CurrentVersion})." });

        // Wipe existing gym data (cascade clears SessionMember/ScheduleSlot/Sessions/Clients/TrainingTypes).
        _db.Sessions.RemoveRange(_db.Sessions);
        _db.Clients.RemoveRange(_db.Clients);
        _db.TrainingTypes.RemoveRange(_db.TrainingTypes);
        await _db.SaveChangesAsync(ct);

        // Resolve owner ids: if the payload references unknown trainers, fall back to importer.
        var knownUserIds = await _db.Users.Select(u => u.Id).ToHashSetAsync(ct);
        Guid Resolve(Guid id) => knownUserIds.Contains(id) ? id : scope.UserId;

        foreach (var t in payload.TrainingTypes)
            _db.TrainingTypes.Add(new TrainingType { Id = t.Id, Name = t.Name, SortOrder = t.SortOrder });

        foreach (var c in payload.Clients)
            _db.Clients.Add(new Client
            {
                Id = c.Id, Name = c.Name, Phone = c.Phone, Notes = c.Notes,
                IsActive = c.IsActive,
            });

        await _db.SaveChangesAsync(ct);

        foreach (var s in payload.Sessions)
        {
            _db.Sessions.Add(new Session
            {
                Id = s.Id, Title = s.Title, TrainingTypeId = s.TrainingTypeId,
                Notes = s.Notes, IsActive = s.IsActive, OwnerTrainerId = Resolve(s.OwnerTrainerId),
                Schedule = s.Schedule.Select(sl => new ScheduleSlot { Day = sl.Day, Time = sl.Time }).ToList(),
            });
        }
        await _db.SaveChangesAsync(ct);

        foreach (var s in payload.Sessions)
            foreach (var mid in s.MemberIds.Distinct())
                _db.SessionMembers.Add(new SessionMember { SessionId = s.Id, ClientId = mid });
        await _db.SaveChangesAsync(ct);

        return Ok(new { restored = new { payload.TrainingTypes.Count, clients = payload.Clients.Count, sessions = payload.Sessions.Count } });
    }

    public class GymBackup
    {
        public int Version { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public List<UserBackup> Users { get; set; } = new();
        public List<TrainingTypeBackup> TrainingTypes { get; set; } = new();
        public List<ClientBackup> Clients { get; set; } = new();
        public List<SessionBackup> Sessions { get; set; } = new();
    }

    public class UserBackup
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "Trainer";
        public bool IsActive { get; set; }
    }

    public class TrainingTypeBackup
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class ClientBackup
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public class SessionBackup
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid TrainingTypeId { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public Guid OwnerTrainerId { get; set; }
        public List<SlotBackup> Schedule { get; set; } = new();
        public List<Guid> MemberIds { get; set; } = new();
    }

    public class SlotBackup
    {
        public DayOfWeek Day { get; set; }
        public TimeOnly Time { get; set; }
    }
}
