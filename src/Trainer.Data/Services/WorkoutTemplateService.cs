using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class WorkoutTemplateService : IWorkoutTemplateService
{
    private readonly TrainerDbContext _db;

    public WorkoutTemplateService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<WorkoutTemplate>> GetAllAsync(CancellationToken ct = default) =>
        await _db.WorkoutTemplates.AsNoTracking()
            .Include(t => t.Items.OrderBy(i => i.Order))
                .ThenInclude(i => i.Exercise)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public Task<WorkoutTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.WorkoutTemplates
            .Include(t => t.Items.OrderBy(i => i.Order))
                .ThenInclude(i => i.Exercise)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<WorkoutTemplate> CreateAsync(WorkoutTemplate template, CancellationToken ct = default)
    {
        NormalizeOrder(template.Items);
        _db.WorkoutTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return template;
    }

    public async Task UpdateAsync(WorkoutTemplate template, CancellationToken ct = default)
    {
        _db.WorkoutTemplates.Update(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.WorkoutTemplates.FindAsync(new object[] { id }, ct);
        if (t is null) return;
        _db.WorkoutTemplates.Remove(t);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<WorkoutTemplate> DuplicateAsync(Guid templateId, string? newName = null, CancellationToken ct = default)
    {
        var src = await _db.WorkoutTemplates
            .Include(t => t.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        var copy = new WorkoutTemplate
        {
            Name = newName ?? $"{src.Name} (копия)",
            Notes = src.Notes,
            Items = src.Items
                .OrderBy(i => i.Order)
                .Select(i => new TemplateItem
                {
                    ExerciseId = i.ExerciseId,
                    Order = i.Order,
                    Rounds = i.Rounds,
                    RoundDurationSec = i.RoundDurationSec,
                    RestBetweenRoundsSec = i.RestBetweenRoundsSec,
                    Sets = i.Sets,
                    Reps = i.Reps,
                    Weight = i.Weight,
                    DurationSec = i.DurationSec,
                    Combo = i.Combo,
                    Intensity = i.Intensity,
                    Notes = i.Notes,
                })
                .ToList(),
        };

        _db.WorkoutTemplates.Add(copy);
        await _db.SaveChangesAsync(ct);
        return copy;
    }

    public async Task AddItemAsync(Guid templateId, TemplateItem item, CancellationToken ct = default)
    {
        var exists = await _db.WorkoutTemplates.AnyAsync(t => t.Id == templateId, ct);
        if (!exists) throw new InvalidOperationException($"Template {templateId} not found");

        item.TemplateId = templateId;
        if (item.Order <= 0)
        {
            var maxOrder = await _db.TemplateItems
                .Where(i => i.TemplateId == templateId)
                .Select(i => (int?)i.Order)
                .MaxAsync(ct) ?? 0;
            item.Order = maxOrder + 1;
        }

        _db.TemplateItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateItemAsync(TemplateItem item, CancellationToken ct = default)
    {
        _db.TemplateItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.TemplateItems.FindAsync(new object[] { itemId }, ct);
        if (item is null) return;
        _db.TemplateItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReorderItemsAsync(Guid templateId, IReadOnlyList<Guid> orderedItemIds, CancellationToken ct = default)
    {
        var items = await _db.TemplateItems
            .Where(i => i.TemplateId == templateId)
            .ToListAsync(ct);

        var index = orderedItemIds
            .Select((id, i) => (id, i))
            .ToDictionary(x => x.id, x => x.i + 1);

        foreach (var item in items)
        {
            if (index.TryGetValue(item.Id, out var order))
                item.Order = order;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static void NormalizeOrder(List<TemplateItem> items)
    {
        var i = 1;
        foreach (var item in items.OrderBy(x => x.Order))
            item.Order = i++;
    }
}
