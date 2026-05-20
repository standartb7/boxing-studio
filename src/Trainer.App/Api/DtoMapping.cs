using Trainer.Contracts;
using Trainer.Core.Entities;

namespace Trainer.App.Api;

/// <summary>
/// Maps wire DTOs (Trainer.Contracts) to UI-facing entities (Trainer.Core.Entities).
/// We keep the entities as the binding source so existing ViewModels/XAML keep working
/// without changes.
/// </summary>
public static class DtoMapping
{
    public static Client ToEntity(this ClientDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Phone = d.Phone,
        Notes = d.Notes,
        IsActive = d.IsActive,
        OwnerTrainerId = d.OwnerTrainerId,
        OwnerDisplayName = d.OwnerDisplayName,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
    };

    public static TrainingType ToEntity(this TrainingTypeDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        SortOrder = d.SortOrder,
    };

    public static Session ToEntity(this SessionDto d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        TrainingTypeId = d.TrainingTypeId,
        Notes = d.Notes,
        IsActive = d.IsActive,
        OwnerTrainerId = d.OwnerTrainerId,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        Schedule = d.Schedule.Select(sl => new ScheduleSlot
        {
            SessionId = d.Id, Day = sl.Day, Time = sl.Time,
        }).ToList(),
        // Members hydrated lazily — page-level code calls a separate Clients endpoint when needed.
        Members = d.MemberIds.Select(id => new Client { Id = id }).ToList(),
    };
}
