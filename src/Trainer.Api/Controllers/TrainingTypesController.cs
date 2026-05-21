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
[Route("api/training-types")]
public class TrainingTypesController : ControllerBase
{
    private readonly TrainerDbContext _db;

    public TrainingTypesController(TrainerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TrainingTypeDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.TrainingTypes.AsNoTracking()
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => t.ToDto())
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
    public async Task<ActionResult<TrainingTypeDto>> Create(CreateTrainingTypeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var name = req.Name.Trim();
        var existing = await _db.TrainingTypes.FirstOrDefaultAsync(t => t.Name == name, ct);
        if (existing is not null) return Conflict(new { error = $"Группа «{name}» уже существует." });

        var maxOrder = await _db.TrainingTypes.AnyAsync(ct)
            ? await _db.TrainingTypes.MaxAsync(t => (int?)t.SortOrder, ct) ?? 0
            : 0;
        var entity = new TrainingType { Name = name, SortOrder = maxOrder + 1 };
        _db.TrainingTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(entity.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
    public async Task<ActionResult<TrainingTypeDto>> Rename(Guid id, RenameTrainingTypeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewName))
            return BadRequest(new { error = "newName is required." });

        var entity = await _db.TrainingTypes.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return NotFound();

        var name = req.NewName.Trim();
        if (await _db.TrainingTypes.AnyAsync(t => t.Id != id && t.Name == name, ct))
            return Conflict(new { error = $"Группа «{name}» уже существует." });

        entity.Name = name;
        await _db.SaveChangesAsync(ct);
        return Ok(entity.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.TrainingTypes.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return NotFound();

        var hasSessions = await _db.Sessions.AnyAsync(s => s.TrainingTypeId == id, ct);
        if (hasSessions) return BadRequest(new { error = "Нельзя удалить тип, в котором есть сессии." });

        _db.TrainingTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
