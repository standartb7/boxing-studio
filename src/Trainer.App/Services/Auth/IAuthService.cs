namespace Trainer.App.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string? displayName, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task LogoutAsync();
}
