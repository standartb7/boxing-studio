using Microsoft.EntityFrameworkCore;
using Trainer.Api.Auth;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api;

/// <summary>
/// One-shot CLI: creates the very first HeadTrainer account in an otherwise-empty database.
/// Idempotent on existing email — updates the password instead of failing, so it doubles as
/// a 'reset HeadTrainer password' tool.
///
/// Usage: dotnet run --project src/Trainer.Api seed-head-trainer &lt;email&gt; &lt;password&gt; &lt;displayName&gt;
/// </summary>
public static class SeedHeadTrainer
{
    public const string Command = "seed-head-trainer";

    public static async Task<int> RunAsync(string[] args, string? connectionString)
    {
        if (args.Length < 4)
        {
            Console.Error.WriteLine($"Usage: {Command} <email> <password> <displayName>");
            return 2;
        }
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("ConnectionStrings:Postgres is not configured.");
            return 2;
        }

        var email = args[1].Trim();
        var password = args[2];
        var displayName = args[3].Trim();

        var opts = new DbContextOptionsBuilder<TrainerDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsAssembly("Trainer.Api"))
            .Options;
        await using var db = new TrainerDbContext(opts);
        await db.Database.MigrateAsync();

        var hasher = new PasswordHasher();
        var hash = hasher.Hash(password);

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is null)
        {
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = hash,
                Role = UserRole.HeadTrainer,
                DisplayName = displayName,
                IsActive = true,
            });
            Console.WriteLine($"Created HeadTrainer {email}");
        }
        else
        {
            existing.PasswordHash = hash;
            existing.Role = UserRole.HeadTrainer;
            existing.DisplayName = displayName;
            existing.IsActive = true;
            existing.InviteCode = null;
            existing.InviteCodeExpiresAt = null;
            Console.WriteLine($"Updated existing user {email} -> HeadTrainer with new password");
        }
        await db.SaveChangesAsync();
        return 0;
    }
}
