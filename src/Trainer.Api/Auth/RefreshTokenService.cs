using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api.Auth;

/// <summary>
/// Issues, rotates, and revokes refresh tokens. Tokens are stored only as SHA-256 hashes;
/// the plaintext is returned once at issue time. Rotation chain detection: if a revoked
/// token is presented for refresh, the entire descendant chain is revoked.
/// </summary>
public class RefreshTokenService
{
    private readonly TrainerDbContext _db;
    private readonly AuthOptions _opts;

    public RefreshTokenService(TrainerDbContext db, IOptions<AuthOptions> opts)
    {
        _db = db;
        _opts = opts.Value;
    }

    public async Task<(string Plaintext, RefreshToken Entity)> IssueAsync(
        Guid userId, string deviceId, CancellationToken ct)
    {
        var plaintext = GeneratePlaintext();
        var entity = new RefreshToken
        {
            UserId = userId,
            DeviceId = deviceId,
            TokenHash = HashToken(plaintext),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_opts.RefreshTokenDays),
        };
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (plaintext, entity);
    }

    /// <summary>
    /// Validate + rotate. Returns the new refresh token plaintext and the user, or null if
    /// the token is unknown/expired. If the token is already revoked, the whole chain is
    /// torched defensively.
    /// </summary>
    public async Task<(string NewPlaintext, User User)?> RotateAsync(
        string plaintext, string deviceId, CancellationToken ct)
    {
        var hash = HashToken(plaintext);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null) return null;
        if (existing.DeviceId != deviceId) return null;

        if (existing.RevokedAt is not null)
        {
            // Replay of a rotated token — revoke the entire forward chain.
            await RevokeChainAsync(existing, ct);
            return null;
        }

        if (existing.ExpiresAt <= DateTimeOffset.UtcNow) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId, ct);
        if (user is null || !user.IsActive) return null;

        var (newPlaintext, newEntity) = await IssueAsync(user.Id, deviceId, ct);

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.ReplacedByTokenId = newEntity.Id;
        await _db.SaveChangesAsync(ct);

        return (newPlaintext, user);
    }

    public async Task RevokeAsync(string plaintext, CancellationToken ct)
    {
        var hash = HashToken(plaintext);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (existing is null) return;
        existing.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task RevokeChainAsync(RefreshToken start, CancellationToken ct)
    {
        var current = start;
        var now = DateTimeOffset.UtcNow;
        while (current.ReplacedByTokenId is { } nextId)
        {
            var next = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == nextId, ct);
            if (next is null) break;
            if (next.RevokedAt is null) next.RevokedAt = now;
            current = next;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static string GeneratePlaintext()
    {
        // 256 bits of entropy, base64url-encoded.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string plaintext)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
