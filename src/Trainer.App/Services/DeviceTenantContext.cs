using Trainer.Core.Abstractions;

namespace Trainer.App.Services;

/// <summary>
/// Временная реализация ITenantContext для одиночного устройства.
/// Заменится на реализацию, читающую TenantId из JWT, когда подключим backend + auth.
/// </summary>
public class DeviceTenantContext : ITenantContext
{
    private const string PrefKey = "tenant_id";
    private readonly Guid _tenantId;

    public DeviceTenantContext()
    {
        var stored = Preferences.Default.Get(PrefKey, string.Empty);
        if (Guid.TryParse(stored, out var existing))
        {
            _tenantId = existing;
        }
        else
        {
            _tenantId = Guid.NewGuid();
            Preferences.Default.Set(PrefKey, _tenantId.ToString());
        }
    }

    public Guid CurrentTenantId => _tenantId;
}
