using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Trainer.Core.Entities;

namespace Trainer.Api.Auth;

public class JwtService
{
    public const string ClaimRole = "role";
    public const string ClaimUserId = ClaimTypes.NameIdentifier;
    public const string ClaimEmail = ClaimTypes.Email;

    private readonly AuthOptions _opts;

    public JwtService(IOptions<AuthOptions> opts) => _opts = opts.Value;

    public (string Token, DateTimeOffset ExpiresAt) IssueAccessToken(User user)
    {
        var keyBytes = Convert.FromBase64String(_opts.JwtSigningKey);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(ClaimUserId, user.Id.ToString()),
            new(ClaimEmail, user.Email),
            new(ClaimRole, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _opts.JwtIssuer,
            audience: _opts.JwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token),
                new DateTimeOffset(expires, TimeSpan.Zero));
    }

    public TokenValidationParameters BuildValidationParameters()
    {
        var keyBytes = Convert.FromBase64String(_opts.JwtSigningKey);
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _opts.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _opts.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimRole,
            NameClaimType = ClaimEmail,
        };
    }
}
