using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class ScheduleService : IScheduleService
{
    private readonly TrainerDbContext _db;

    public ScheduleService(TrainerDbContext db) => _db = db;

    public async Task<IReadOnlyList<ScheduledWorkout>> GetForDayAsync(DateOnly day, CancellationToken ct = default)
    {
        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(1);
        return await _db.ScheduledWorkouts.AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.StartAt >= from && w.StartAt < to)
            .OrderBy(w => w.StartAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ScheduledWorkout>> GetForClientAsync(Guid clientId, CancellationToken ct = default) =>
        await _db.ScheduledWorkouts.AsNoTracking()
            .Where(w => w.ClientId == clientId)
            .OrderByDescending(w => w.StartAt)
            .ToListAsync(ct);

    public async Task<ScheduledWorkout> ScheduleFromTemplateAsync(
        Guid clientId, DateTimeOffset startAt, Guid templateId, CancellationToken ct = default)
    {
        var template = await _db.WorkoutTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        var workout = new ScheduledWorkout
        {
            ClientId = clientId,
            StartAt = startAt,
            TemplateId = templateId,
            Items = template.Items.Select(i => new WorkoutItem
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
            }).ToList(),
        };

        _db.ScheduledWorkouts.Add(workout);
        await _db.SaveChangesAsync(ct);
        return workout;
    }

    public async Task<ScheduledWorkout> ScheduleCustomAsync(ScheduledWorkout workout, CancellationToken ct = default)
    {
        _db.ScheduledWorkouts.Add(workout);
        await _db.SaveChangesAsync(ct);
        return workout;
    }

    public async Task MarkStatusAsync(Guid workoutId, WorkoutStatus status, CancellationToken ct = default)
    {
        var w = await _db.ScheduledWorkouts.FindAsync(new object[] { workoutId }, ct);
        if (w is null) return;
        w.Status = status;
        await _db.SaveChangesAsync(ct);
    }
}
