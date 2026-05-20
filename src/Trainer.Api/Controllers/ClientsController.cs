using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Mapping;
using Trainer.Contracts;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api.Controllers;

/// <summary>
/// Clients are gym-wide — every active trainer can read and edit any client. A
/// client may train with several trainers in parallel (group with one, personal
/// with another), so ownership belongs to Session, not Client.
/// </summary>
[ApiController]
[Authorize]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly TrainerDbContext _db;

    public ClientsController(TrainerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Clients.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken ct)
    {
        var c = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(c.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(CreateClientRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var name = req.Name.Trim();
        if (await _db.Clients.AnyAsync(c => c.Name == name, ct))
            return Conflict(new { error = $"Клиент с именем «{name}» уже существует." });

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

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
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
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
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
