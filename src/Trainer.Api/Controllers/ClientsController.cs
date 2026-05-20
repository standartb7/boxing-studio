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

    private IQueryable<Client> Scoped() =>
        _scope.IsHeadTrainer
            ? _db.Clients
            : _db.Clients.Where(c => c.OwnerTrainerId == _scope.UserId);

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetAll(CancellationToken ct)
    {
        // Left-join on User so we can ship OwnerDisplayName alongside the Guid. EF Core
        // translates GroupJoin + DefaultIfEmpty into a LEFT JOIN, so clients owned by a
        // user that was later hard-deleted still show up with OwnerDisplayName = null.
        var rows = await (
            from c in Scoped().AsNoTracking()
            where c.IsActive
            join u in _db.Users on c.OwnerTrainerId equals u.Id into us
            from u in us.DefaultIfEmpty()
            orderby c.Name
            select new { Client = c, OwnerName = u != null ? u.DisplayName : null }
        ).ToListAsync(ct);

        return Ok(rows.Select(r => r.Client.ToDto(r.OwnerName)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken ct)
    {
        var row = await (
            from c in Scoped().AsNoTracking()
            where c.Id == id
            join u in _db.Users on c.OwnerTrainerId equals u.Id into us
            from u in us.DefaultIfEmpty()
            select new { Client = c, OwnerName = u != null ? u.DisplayName : null }
        ).FirstOrDefaultAsync(ct);

        return row is null ? NotFound() : Ok(row.Client.ToDto(row.OwnerName));
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(CreateClientRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        // Owner: HeadTrainer may specify; trainer is forced to self.
        var ownerId = _scope.IsHeadTrainer && req.OwnerTrainerId is { } v
            ? v
            : _scope.UserId;
        if (_scope.IsHeadTrainer && req.OwnerTrainerId is { } v2)
        {
            var ownerExists = await _db.Users.AnyAsync(u => u.Id == v2 && u.IsActive, ct);
            if (!ownerExists) return BadRequest(new { error = "Owner trainer not found or inactive." });
        }

        var name = req.Name.Trim();
        if (await _db.Clients.AnyAsync(c => c.Name == name, ct))
            return Conflict(new { error = $"Клиент с именем «{name}» уже существует." });

        var client = new Client
        {
            Name = name,
            Phone = req.Phone?.Trim(),
            Notes = req.Notes,
            IsActive = true,
            OwnerTrainerId = ownerId,
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);

        var ownerName = await _db.Users.Where(u => u.Id == ownerId)
            .Select(u => u.DisplayName).FirstOrDefaultAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client.ToDto(ownerName));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientDto>> Update(Guid id, UpdateClientRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

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

        var ownerName = await _db.Users.Where(u => u.Id == client.OwnerTrainerId)
            .Select(u => u.DisplayName).FirstOrDefaultAsync(ct);
        return Ok(client.ToDto(ownerName));
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

        // Soft delete: mark inactive so historical references stay intact.
        client.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reassign")]
    [Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
    public async Task<ActionResult<ClientDto>> Reassign(Guid id, ReassignClientRequest req, CancellationToken ct)
    {
        var newOwner = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.NewOwnerTrainerId && u.IsActive, ct);
        if (newOwner is null) return BadRequest(new { error = "Target trainer not found or inactive." });

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return NotFound();

        client.OwnerTrainerId = newOwner.Id;
        await _db.SaveChangesAsync(ct);
        return Ok(client.ToDto(newOwner.DisplayName));
    }
}
