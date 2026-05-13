namespace Trainer.App.Services.Auth;

/// <summary>
/// Хранит текущий JWT и связанные с ним данные.
/// Сначала пробует SecureStorage (Keychain / Android KeyStore). Если упало (например,
/// dev-сборка Mac Catalyst без правильной подписи и entitlement) — fallback на Preferences.
/// </summary>
public class AuthState
{
    private const string TokenKey = "auth_token";
    private const string ExpiresAtKey = "auth_expires_at";
    private const string TenantKey = "auth_tenant_id";
    private const string EmailKey = "auth_email";

    public event EventHandler? Changed;

    public string? Token { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public Guid? TenantId { get; private set; }
    public string? Email { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(Token) && ExpiresAt is not null && ExpiresAt > DateTimeOffset.UtcNow;

    public async Task LoadAsync()
    {
        Token = await ReadAsync(TokenKey);
        var expRaw = await ReadAsync(ExpiresAtKey);
        ExpiresAt = DateTimeOffset.TryParse(expRaw, out var exp) ? exp : null;
        var tenantRaw = await ReadAsync(TenantKey);
        TenantId = Guid.TryParse(tenantRaw, out var t) ? t : null;
        Email = await ReadAsync(EmailKey);
    }

    public async Task SetAsync(AuthResponse response)
    {
        Token = response.AccessToken;
        ExpiresAt = response.ExpiresAt;
        TenantId = response.TenantId;
        Email = response.Email;

        await WriteAsync(TokenKey, response.AccessToken);
        await WriteAsync(ExpiresAtKey, response.ExpiresAt.ToString("O"));
        await WriteAsync(TenantKey, response.TenantId.ToString());
        await WriteAsync(EmailKey, response.Email);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Token = null;
        ExpiresAt = null;
        TenantId = null;
        Email = null;

        Remove(TokenKey);
        Remove(ExpiresAtKey);
        Remove(TenantKey);
        Remove(EmailKey);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    // --- storage with fallback ---

    private static async Task<string?> ReadAsync(string key)
    {
        try
        {
            var v = await SecureStorage.Default.GetAsync(key);
            if (v is not null) return v;
        }
        catch
        {
            // SecureStorage недоступен (нет entitlement, не подписан и т.п.) — fallback
        }
        return Preferences.Default.Get<string?>(key, null);
    }

    private static async Task WriteAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
            return;
        }
        catch
        {
            // fallback
        }
        Preferences.Default.Set(key, value);
    }

    private static void Remove(string key)
    {
        try { SecureStorage.Default.Remove(key); } catch { /* ignore */ }
        Preferences.Default.Remove(key);
    }
}
