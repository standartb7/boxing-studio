namespace Trainer.Core.Entities;

public enum UserRole
{
    Trainer = 0,
    HeadTrainer = 1,
}

public class User : EntityBase
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Trainer;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    // Set when the user is invited and not yet activated. Cleared on accept-invite.
    public string? InviteCode { get; set; }
    public DateTimeOffset? InviteCodeExpiresAt { get; set; }
}
