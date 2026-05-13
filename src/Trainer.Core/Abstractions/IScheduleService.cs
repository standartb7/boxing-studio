using Trainer.Core.Entities;

namespace Trainer.Core.Abstractions;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduledWorkout>> GetForDayAsync(DateOnly day, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledWorkout>> GetForClientAsync(Guid clientId, CancellationToken ct = default);
    Task<ScheduledWorkout> ScheduleFromTemplateAsync(Guid clientId, DateTimeOffset startAt, Guid templateId, CancellationToken ct = default);
    Task<ScheduledWorkout> ScheduleCustomAsync(ScheduledWorkout workout, CancellationToken ct = default);
    Task MarkStatusAsync(Guid workoutId, WorkoutStatus status, CancellationToken ct = default);
}
