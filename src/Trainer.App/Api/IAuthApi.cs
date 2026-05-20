using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

/// <summary>
/// Anonymous auth endpoints. Registered WITHOUT AuthDelegatingHandler so AuthService
/// can call us during refresh without triggering a DI cycle.
/// HeadTrainer-only invite() is intentionally on a separate interface (Phase E).
/// </summary>
public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest req, CancellationToken ct = default);

    [Post("/api/auth/refresh")]
    Task<TokenPair> RefreshAsync([Body] RefreshRequest req, CancellationToken ct = default);

    [Post("/api/auth/logout")]
    Task LogoutAsync([Body] LogoutRequest req, CancellationToken ct = default);

    [Post("/api/auth/accept-invite")]
    Task<LoginResponse> AcceptInviteAsync([Body] AcceptInviteRequest req, CancellationToken ct = default);
}
