using Trainer.Core.Abstractions;

namespace Trainer.Api.Tenancy;

/// <summary>
/// Stub для design-time (dotnet ef migrations). Реальный фильтр на runtime подставит HttpHeaderTenantContext / JwtTenantContext.
/// </summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid CurrentTenantId => Guid.Empty;
}
