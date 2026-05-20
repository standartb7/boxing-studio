using System.Security.Claims;
using Trainer.Core.Entities;

namespace Trainer.Api.Auth;

public interface ITrainerScope
{
    Guid UserId { get; }
    UserRole Role { get; }
    bool IsHeadTrainer { get; }
}

/// <summary>
/// Request-scoped: reads JWT claims set by JwtBearer middleware.
/// Throws if accessed without an authenticated principal — controllers should be
/// protected by [Authorize] so this never happens silently.
/// </summary>
public class HttpTrainerScope : ITrainerScope
{
    public Guid UserId { get; }
    public UserRole Role { get; }
    public bool IsHeadTrainer => Role == UserRole.HeadTrainer;

    public HttpTrainerScope(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User
            ?? throw new InvalidOperationException("No HttpContext.");
        var idClaim = user.FindFirstValue(JwtService.ClaimUserId)
            ?? throw new InvalidOperationException("Missing user id claim.");
        UserId = Guid.Parse(idClaim);

        var roleClaim = user.FindFirstValue(JwtService.ClaimRole) ?? UserRole.Trainer.ToString();
        Role = Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var parsed)
            ? parsed : UserRole.Trainer;
    }
}

public static class AuthPolicies
{
    public const string HeadTrainerOnly = nameof(HeadTrainerOnly);
}
