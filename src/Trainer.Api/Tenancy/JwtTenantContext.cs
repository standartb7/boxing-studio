using Trainer.Api.Auth;
using Trainer.Core.Abstractions;

namespace Trainer.Api.Tenancy;

/// <summary>
/// Читает TenantId из claim 'tenant_id' валидированного JWT. Для анонимных запросов
/// (auth/register, auth/login) вернёт Guid.Empty — у них query filter и так не должен срабатывать.
/// </summary>
public class JwtTenantContext : ITenantContext
{
    private readonly Guid _tenantId;

    public JwtTenantContext(IHttpContextAccessor accessor)
    {
        var http = accessor.HttpContext;
        var claim = http?.User?.FindFirst(JwtSettings.TenantClaim);
        _tenantId = (claim is not null && Guid.TryParse(claim.Value, out var parsed))
            ? parsed
            : Guid.Empty;
    }

    public Guid CurrentTenantId => _tenantId;
}
