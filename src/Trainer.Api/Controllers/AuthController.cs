using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trainer.Api.Auth;
using Trainer.Api.Mapping;
using Trainer.Contracts;
using Trainer.Core.Entities;
using Trainer.Infrastructure;

namespace Trainer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly TrainerDbContext _db;
    private readonly PasswordHasher _hasher;
    private readonly JwtService _jwt;
    private readonly RefreshTokenService _refresh;
    private readonly AuthOptions _opts;

    public AuthController(
        TrainerDbContext db,
        PasswordHasher hasher,
        JwtService jwt,
        RefreshTokenService refresh,
        IOptions<AuthOptions> opts)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _refresh = refresh;
        _opts = opts.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password)
            || string.IsNullOrWhiteSpace(req.DeviceId))
        {
            return BadRequest(new { error = "Email, password, and deviceId are required." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.Trim(), ct);
        if (user is null || !user.IsActive || !_hasher.Verify(req.Password, user.PasswordHash))
        {
            // Same response for unknown email and bad password — don't leak account existence.
            return Unauthorized(new { error = "Invalid credentials." });
        }
        if (!string.IsNullOrEmpty(user.InviteCode))
        {
            return Unauthorized(new { error = "Account is pending invite acceptance." });
        }

        var (access, accessExp) = _jwt.IssueAccessToken(user);
        var (refresh, _) = await _refresh.IssueAsync(user.Id, req.DeviceId, ct);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new LoginResponse
        {
            AccessToken = access,
            AccessTokenExpiresAt = accessExp,
            RefreshToken = refresh,
            User = user.ToDto(),
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenPair>> Refresh(RefreshRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken) || string.IsNullOrWhiteSpace(req.DeviceId))
            return BadRequest(new { error = "refreshToken and deviceId are required." });

        var result = await _refresh.RotateAsync(req.RefreshToken, req.DeviceId, ct);
        if (result is null) return Unauthorized(new { error = "Invalid or expired refresh token." });

        var (newRefresh, user) = result.Value;
        var (access, accessExp) = _jwt.IssueAccessToken(user);

        return Ok(new TokenPair
        {
            AccessToken = access,
            AccessTokenExpiresAt = accessExp,
            RefreshToken = newRefresh,
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return NoContent();
        await _refresh.RevokeAsync(req.RefreshToken, ct);
        return NoContent();
    }

    [HttpPost("invite")]
    [Authorize(Policy = AuthPolicies.HeadTrainerOnly)]
    public async Task<ActionResult<InviteResponse>> Invite(InviteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { error = "Email and displayName are required." });

        if (!EntityMappings.TryParseRole(req.Role, out var role))
            return BadRequest(new { error = $"Unknown role '{req.Role}'." });

        var email = req.Email.Trim();
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null)
            return Conflict(new { error = "A user with this email already exists." });

        var code = InviteCodeGenerator.Generate();
        var expires = DateTimeOffset.UtcNow.AddDays(_opts.InviteCodeDays);
        var user = new User
        {
            Email = email,
            DisplayName = req.DisplayName.Trim(),
            Role = role,
            IsActive = false,                  // becomes active on accept-invite
            PasswordHash = string.Empty,
            InviteCode = code,
            InviteCodeExpiresAt = expires,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new InviteResponse
        {
            InviteCode = code,
            ExpiresAt = expires,
            UserId = user.Id,
        });
    }

    [HttpPost("accept-invite")]
    public async Task<ActionResult<LoginResponse>> AcceptInvite(AcceptInviteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.InviteCode)
            || string.IsNullOrWhiteSpace(req.Password)
            || string.IsNullOrWhiteSpace(req.DeviceId))
        {
            return BadRequest(new { error = "inviteCode, password, and deviceId are required." });
        }

        var code = req.InviteCode.Trim().ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.InviteCode == code, ct);
        if (user is null) return Unauthorized(new { error = "Invalid invite code." });
        if (user.InviteCodeExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
            return Unauthorized(new { error = "Invite code has expired." });

        user.PasswordHash = _hasher.Hash(req.Password);
        user.InviteCode = null;
        user.InviteCodeExpiresAt = null;
        user.IsActive = true;
        user.LastLoginAt = DateTimeOffset.UtcNow;

        var (access, accessExp) = _jwt.IssueAccessToken(user);
        var (refresh, _) = await _refresh.IssueAsync(user.Id, req.DeviceId, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new LoginResponse
        {
            AccessToken = access,
            AccessTokenExpiresAt = accessExp,
            RefreshToken = refresh,
            User = user.ToDto(),
        });
    }
}
