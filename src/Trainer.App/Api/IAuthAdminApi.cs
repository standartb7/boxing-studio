using Refit;
using Trainer.Contracts;

namespace Trainer.App.Api;

/// <summary>
/// HeadTrainer-only auth endpoints. Lives in the authenticated handler so JWT is
/// attached — separate from IAuthApi to avoid the AuthService -> IAuthApi -> handler
/// DI cycle that would otherwise occur.
/// </summary>
public interface IAuthAdminApi
{
    [Post("/api/auth/invite")]
    Task<InviteResponse> InviteAsync([Body] InviteRequest req, CancellationToken ct = default);
}
