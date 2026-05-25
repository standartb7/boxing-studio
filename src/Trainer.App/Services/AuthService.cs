using System.Security.Cryptography;
using Trainer.App.Api;
using Trainer.Contracts;

namespace Trainer.App.Services;

/// <summary>
/// Holds the access token in memory and the refresh token + email + deviceId in
/// SecureStorage. The HTTP delegating handler asks here for the current access token
/// and triggers a refresh on 401.
/// </summary>
public class AuthService
{
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string EmailKey = "auth_email";
    private const string DisplayNameKey = "auth_display_name";
    private const string RoleKey = "auth_role";
    private const string UserIdKey = "auth_user_id";
    private const string DeviceIdKey = "auth_device_id";

    private readonly IAuthApi _api;
    private readonly IAuthMeApi _meApi;

    private string? _accessToken;
    private DateTimeOffset _accessExpiresAt;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(IAuthApi api, IAuthMeApi meApi)
    {
        _api = api;
        _meApi = meApi;
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(GetStoredRefreshToken());
    public string? CurrentUserEmail => Preferences.Default.Get<string?>(EmailKey, null);
    public string? CurrentUserDisplayName => Preferences.Default.Get<string?>(DisplayNameKey, null);
    public string? CurrentUserRole => Preferences.Default.Get<string?>(RoleKey, null);
    public bool IsHeadTrainer => CurrentUserRole == "HeadTrainer";
    public Guid? CurrentUserId =>
        Guid.TryParse(Preferences.Default.Get<string?>(UserIdKey, null), out var id) ? id : null;

    public async Task LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var deviceId = await GetOrCreateDeviceIdAsync();
        var resp = await _api.LoginAsync(new LoginRequest
        {
            Email = email.Trim(),
            Password = password,
            DeviceId = deviceId,
        }, ct);
        ApplyLogin(resp);
    }

    public async Task AcceptInviteAsync(string code, string password, CancellationToken ct = default)
    {
        var deviceId = await GetOrCreateDeviceIdAsync();
        var resp = await _api.AcceptInviteAsync(new AcceptInviteRequest
        {
            InviteCode = code.Trim(),
            Password = password,
            DeviceId = deviceId,
        }, ct);
        ApplyLogin(resp);
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_accessToken) && _accessExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            return _accessToken;
        return await RefreshAsync(ct);
    }

    public async Task<string?> RefreshAsync(CancellationToken ct = default)
    {
        var refreshToken = GetStoredRefreshToken();
        if (string.IsNullOrEmpty(refreshToken)) return null;

        await _refreshLock.WaitAsync(ct);
        try
        {
            // Re-check after lock acquired in case another caller already refreshed.
            if (!string.IsNullOrEmpty(_accessToken) && _accessExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
                return _accessToken;

            var deviceId = await GetOrCreateDeviceIdAsync();
            try
            {
                var pair = await _api.RefreshAsync(new RefreshRequest
                {
                    RefreshToken = refreshToken!,
                    DeviceId = deviceId,
                }, ct);
                _accessToken = pair.AccessToken;
                _accessExpiresAt = pair.AccessTokenExpiresAt;
                await SecureStorage.Default.SetAsync(RefreshTokenKey, pair.RefreshToken);
                return _accessToken;
            }
            catch
            {
                // Server rejected the refresh token — wipe local creds so the UI sends user to login.
                await LogoutLocalAsync();
                return null;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        await _meApi.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
        }, ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var refreshToken = GetStoredRefreshToken();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try { await _api.LogoutAsync(new LogoutRequest { RefreshToken = refreshToken! }, ct); }
            catch { /* offline / already invalid — wipe locally anyway */ }
        }
        await LogoutLocalAsync();
    }

    private async Task LogoutLocalAsync()
    {
        _accessToken = null;
        _accessExpiresAt = default;
        SecureStorage.Default.Remove(RefreshTokenKey);
        Preferences.Default.Remove(EmailKey);
        Preferences.Default.Remove(DisplayNameKey);
        Preferences.Default.Remove(RoleKey);
        Preferences.Default.Remove(UserIdKey);
        await Task.CompletedTask;
    }

    private void ApplyLogin(LoginResponse resp)
    {
        _accessToken = resp.AccessToken;
        _accessExpiresAt = resp.AccessTokenExpiresAt;
        // Refresh token is the only credential that can mint new access tokens — keep it in SecureStorage.
        SecureStorage.Default.SetAsync(RefreshTokenKey, resp.RefreshToken).GetAwaiter().GetResult();
        Preferences.Default.Set(EmailKey, resp.User.Email);
        Preferences.Default.Set(DisplayNameKey, resp.User.DisplayName);
        Preferences.Default.Set(RoleKey, resp.User.Role);
        Preferences.Default.Set(UserIdKey, resp.User.Id.ToString());
    }

    private string? GetStoredRefreshToken() =>
        // Sync access via .GetAwaiter().GetResult() — SecureStorage on MAUI is async by API but
        // the cost is negligible for our purposes (called rarely, off the UI thread).
        SecureStorage.Default.GetAsync(RefreshTokenKey).GetAwaiter().GetResult();

    private async Task<string> GetOrCreateDeviceIdAsync()
    {
        var existing = await SecureStorage.Default.GetAsync(DeviceIdKey);
        if (!string.IsNullOrEmpty(existing)) return existing;

        var bytes = RandomNumberGenerator.GetBytes(16);
        var id = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        await SecureStorage.Default.SetAsync(DeviceIdKey, id);
        return id;
    }
}
