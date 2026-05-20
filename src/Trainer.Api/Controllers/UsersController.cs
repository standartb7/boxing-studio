using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Api.Mapping;
using Trainer.Contracts;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly TrainerDbContext _db;
    private readonly ITrainerScope _scope;

    public UsersController(TrainerDbContext db, ITrainerScope scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Users.AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .Select(u => u.ToDto())
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return u is null ? NotFound() : Ok(u.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest req, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return NotFound();

        // Role change: refuse if it would leave zero active HeadTrainers.
        if (req.Role is not null)
        {
            if (!EntityMappings.TryParseRole(req.Role, out var newRole))
                return BadRequest(new { error = $"Unknown role '{req.Role}'." });
            if (u.Role == UserRole.HeadTrainer && newRole != UserRole.HeadTrainer
                && await IsLastActiveHeadTrainerAsync(u.Id, ct))
            {
                return BadRequest(new { error = "Нельзя снять роль с последнего главного тренера." });
            }
            u.Role = newRole;
        }

        if (req.IsActive is { } active)
        {
            if (!active && u.Role == UserRole.HeadTrainer
                && await IsLastActiveHeadTrainerAsync(u.Id, ct))
            {
                return BadRequest(new { error = "Нельзя деактивировать последнего главного тренера." });
            }
            u.IsActive = active;
        }

        if (req.DisplayName is not null)
        {
            if (string.IsNullOrWhiteSpace(req.DisplayName))
                return BadRequest(new { error = "displayName cannot be blank." });
            u.DisplayName = req.DisplayName.Trim();
        }

        await _db.SaveChangesAsync(ct);
        return Ok(u.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return NotFound();
        if (u.Role == UserRole.HeadTrainer && await IsLastActiveHeadTrainerAsync(u.Id, ct))
            return BadRequest(new { error = "Нельзя деактивировать последнего главного тренера." });

        u.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> IsLastActiveHeadTrainerAsync(Guid targetId, CancellationToken ct)
    {
        var otherHeadCount = await _db.Users
            .CountAsync(u => u.Id != targetId
                          && u.Role == UserRole.HeadTrainer
                          && u.IsActive
                          && u.InviteCode == null, ct);
        return otherHeadCount == 0;
    }
}
