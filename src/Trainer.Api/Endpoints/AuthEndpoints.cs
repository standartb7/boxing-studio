using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register", async (
            RegisterRequest req,
            TrainerDbContext db,
            IPasswordHasher<User> hasher,
            JwtTokenService tokens,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest("Email and password are required");

            if (req.Password.Length < 8)
                return Results.BadRequest("Password must be at least 8 characters");

            var normalized = req.Email.Trim().ToLowerInvariant();

            var exists = await db.Users.AnyAsync(u => u.Email == normalized, ct);
            if (exists)
                return Results.Conflict("Email is already registered");

            var user = new User
            {
                Email = normalized,
                DisplayName = req.DisplayName?.Trim() ?? string.Empty,
                TenantId = Guid.NewGuid(), // 1 трен = 1 тенант: при регистрации создаётся новый
            };
            user.PasswordHash = hasher.HashPassword(user, req.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            var (token, expiresAt) = tokens.Issue(user);
            return Results.Ok(new AuthResponse(token, expiresAt, user.TenantId, user.Email));
        });

        group.MapPost("/login", async (
            LoginRequest req,
            TrainerDbContext db,
            IPasswordHasher<User> hasher,
            JwtTokenService tokens,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest("Email and password are required");

            var normalized = req.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);

            // унифицированная ошибка, чтобы не выдавать какие email зарегистрированы
            if (user is null)
                return Results.Unauthorized();

            var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (verified == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            // если в будущем сменим алгоритм хеширования — Identity сам подскажет апгрейднуть
            if (verified == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, req.Password);
                await db.SaveChangesAsync(ct);
            }

            var (token, expiresAt) = tokens.Issue(user);
            return Results.Ok(new AuthResponse(token, expiresAt, user.TenantId, user.Email));
        });

        return app;
    }
}
