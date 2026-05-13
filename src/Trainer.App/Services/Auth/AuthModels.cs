namespace Trainer.App.Services.Auth;

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, Guid TenantId, string Email);

public record AuthResult(bool Success, string? ErrorMessage = null);
