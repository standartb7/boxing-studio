namespace Trainer.Api.Auth;

public class JwtSettings
{
    public string Key { get; set; } = "TrainerApiLongSercretKeyForAllNeedsMayBeRequired";
    public string Issuer { get; set; } = "Trainer.Api";
    public string Audience { get; set; } = "Trainer.App";
    public int ExpiresInDays { get; set; } = 7;

    public const string TenantClaim = "tenant_id";
}
