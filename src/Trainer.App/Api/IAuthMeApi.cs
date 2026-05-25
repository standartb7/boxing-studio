using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

/// <summary>
/// Authenticated self-service auth endpoints (anything a logged-in user does to their own account).
/// Separate from IAuthApi to keep that interface anonymous, and from IAuthAdminApi which is
/// HeadTrainer-gated.
/// </summary>
public interface IAuthMeApi
{
    [Post("/api/auth/change-password")]
    Task ChangePasswordAsync([Body] ChangePasswordRequest req, CancellationToken ct = default);
}
