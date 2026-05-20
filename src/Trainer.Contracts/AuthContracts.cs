namespace Trainer.Contracts;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

public class TokenPair
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
}

public class LoginResponse : TokenPair
{
    public UserDto User { get; set; } = new();
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class InviteRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Trainer"; // "Trainer" | "HeadTrainer"
}

public class InviteResponse
{
    public string InviteCode { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid UserId { get; set; }
}

public class AcceptInviteRequest
{
    public string InviteCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}
