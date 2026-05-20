using Trainer.Contracts;
using Trainer.Core.Entities;

namespace Trainer.Api.Mapping;

public static class EntityMappings
{
    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        Role = u.Role.ToString(),
        IsActive = u.IsActive,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt,
    };

    public static ClientDto ToDto(this Client c, string? ownerDisplayName = null) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Notes = c.Notes,
        IsActive = c.IsActive,
        OwnerTrainerId = c.OwnerTrainerId,
        OwnerDisplayName = ownerDisplayName,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };

    public static TrainingTypeDto ToDto(this TrainingType t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        SortOrder = t.SortOrder,
    };

    public static SessionDto ToDto(this Session s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        TrainingTypeId = s.TrainingTypeId,
        Notes = s.Notes,
        IsActive = s.IsActive,
        OwnerTrainerId = s.OwnerTrainerId,
        Schedule = s.Schedule.Select(sl => new ScheduleSlotDto { Day = sl.Day, Time = sl.Time }).ToList(),
        MemberIds = s.Members.Select(m => m.Id).ToList(),
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
    };

    public static bool TryParseRole(string? value, out UserRole role)
    {
        if (Enum.TryParse(value, ignoreCase: true, out role)) return true;
        role = UserRole.Trainer;
        return false;
    }
}
