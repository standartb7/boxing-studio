using Trainer.Core.Abstractions;

namespace Trainer.Api.Tenancy;

/// <summary>
/// Временная реализация tenant context для этапа 1b: TenantId читается из заголовка X-Tenant-Id.
/// Удобно тестировать через Swagger. На этапе 2 заменится на JwtTenantContext, читающий из claim.
/// </summary>
public class HttpHeaderTenantContext : ITenantContext
{
    private const string HeaderName = "X-Tenant-Id";
    private readonly Guid _tenantId;

    public HttpHeaderTenantContext(IHttpContextAccessor accessor)
    {
        var http = accessor.HttpContext;
        if (http != null
            && http.Request.Headers.TryGetValue(HeaderName, out var raw)
            && Guid.TryParse(raw, out var parsed))
        {
            _tenantId = parsed;
        }
        else
        {
            _tenantId = Guid.Empty;
        }
    }

    public Guid CurrentTenantId => _tenantId;
}
