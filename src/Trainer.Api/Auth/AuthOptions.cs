namespace Trainer.Api.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    /// <summary>Base64-encoded HMAC-SHA256 key, at least 32 bytes (256 bits) of entropy.</summary>
    public string JwtSigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
    public int InviteCodeDays { get; set; } = 14;
}
