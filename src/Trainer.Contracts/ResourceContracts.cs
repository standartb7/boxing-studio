namespace Trainer.Contracts;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Trainer";
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class ClientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CreateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

public class UpdateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class TrainingTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CreateTrainingTypeRequest
{
    public string Name { get; set; } = string.Empty;
}

public class RenameTrainingTypeRequest
{
    public string NewName { get; set; } = string.Empty;
}

public class ScheduleSlotDto
{
    public DayOfWeek Day { get; set; }
    public TimeOnly Time { get; set; }
}

public class SessionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid TrainingTypeId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public Guid OwnerTrainerId { get; set; }
    /// <summary>DisplayName of the trainer who owns this session.</summary>
    public string? OwnerDisplayName { get; set; }
    public List<ScheduleSlotDto> Schedule { get; set; } = new();
    public List<Guid> MemberIds { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CreateSessionRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid TrainingTypeId { get; set; }
    public string? Notes { get; set; }
    public Guid? OwnerTrainerId { get; set; }
    public List<ScheduleSlotDto> Schedule { get; set; } = new();
    public List<Guid> MemberIds { get; set; } = new();
}

public class UpdateSessionRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid TrainingTypeId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public List<ScheduleSlotDto> Schedule { get; set; } = new();
    public List<Guid> MemberIds { get; set; } = new();
}
