using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Api.Mapping;
using Trainer.Contracts;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api.Controllers;

/// <summary>
/// Clients live gym-wide in the database — one client can train with several
/// trainers (group with one, personal with another). Visibility is derived from
/// session membership: a regular Trainer only sees clients who are members of at
/// least one session they own. HeadTrainer sees everyone.
/// </summary>
[ApiController]
[Authorize]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly TrainerDbContext _db;
    private readonly ITrainerScope _scope;

    public ClientsController(TrainerDbContext db, ITrainerScope scope)
    {
        _db = db;
        _scope = scope;
    }

    /// <summary>Clients visible to the current user.</summary>
    private IQueryable<Client> Scoped()
    {
        if (_scope.IsHeadTrainer) return _db.Clients;
        var myClientIds =
            from m in _db.SessionMembers
            join s in _db.Sessions on m.SessionId equals s.Id
            where s.OwnerTrainerId == _scope.UserId
            select m.ClientId;
        return _db.Clients.Where(c => myClientIds.Contains(c.Id));
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetAll(CancellationToken ct)
    {
        var list = await Scoped().AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken ct)
    {
        var c = await Scoped().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(c.ToDto());
    }

    /// <summary>
    /// Create-or-get-by-name. If a client with the same name (case-insensitive) already
    /// exists in the gym, return that one with 200 OK instead of erroring — this lets
    /// trainers 'claim' an existing client by typing the name in the '+ Новый' dialog,
    /// even when scope hides the row from their normal list. A soft-deleted match is
    /// reactivated. Phone/Notes from the request are ignored on match to avoid
    /// accidentally trampling another trainer's data.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(CreateClientRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var name = req.Name.Trim();
        var existing = await _db.Clients.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await _db.SaveChangesAsync(ct);
            }
            return Ok(existing.ToDto());
        }

        var client = new Client
        {
            Name = name,
            Phone = req.Phone?.Trim(),
            Notes = req.Notes,
            IsActive = true,
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientDto>> Update(Guid id, UpdateClientRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        // Only clients the caller can see may be edited.
        var client = await Scoped().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return NotFound();

        var name = req.Name.Trim();
        if (await _db.Clients.AnyAsync(c => c.Id != id && c.Name == name, ct))
            return Conflict(new { error = $"Клиент с именем «{name}» уже существует." });

        client.Name = name;
        client.Phone = req.Phone?.Trim();
        client.Notes = req.Notes;
        client.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(client.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var client = await Scoped().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return NotFound();

        var hasActiveSessions = await _db.SessionMembers
            .Where(m => m.ClientId == id)
            .Join(_db.Sessions, m => m.SessionId, s => s.Id, (m, s) => s.IsActive)
            .AnyAsync(active => active, ct);
        if (hasActiveSessions)
            return BadRequest(new { error = "Клиент состоит в активных сессиях. Сначала удалите его из сессий." });

        // Soft delete so historical references stay intact.
        client.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
