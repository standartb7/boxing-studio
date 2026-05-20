namespace Trainer.Core.Entities;

public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    // For rotation-chain detection: when this token is rotated, store the id of the new token.
    // If a revoked token is presented again, the entire chain is revoked.
    public Guid? ReplacedByTokenId { get; set; }
}
