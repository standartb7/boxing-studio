using Microsoft.Extensions.DependencyInjection;
using Trainer.Api.Auth;
using Trainer.Core.Entities;
using Trainer.Data;

namespace Trainer.Api.Tests;

/// <summary>Helpers to seed users and obtain access tokens without going through the HTTP login endpoint.</summary>
public static class TestData
{
    public static async Task<User> SeedUserAsync(
        TestApiFactory factory, string email, string password, UserRole role,
        bool isActive = true, string displayName = "Test")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = role,
            DisplayName = displayName,
            IsActive = isActive,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<(User User, Guid Id)> SeedClientAsync(
        TestApiFactory factory, Guid ownerTrainerId, string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
        var client = new Client
        {
            Name = name,
            OwnerTrainerId = ownerTrainerId,
            IsActive = true,
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return (null!, client.Id);
    }

    public static string IssueAccessToken(TestApiFactory factory, User user)
    {
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<JwtService>();
        return jwt.IssueAccessToken(user).Token;
    }
}
