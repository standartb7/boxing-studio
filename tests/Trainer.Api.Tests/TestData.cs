using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Api.Auth;
using Trainer.Core.Entities;
using Trainer.Infrastructure;

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

    public static async Task<Guid> SeedClientAsync(TestApiFactory factory, string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
        var client = new Client { Name = name, IsActive = true };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client.Id;
    }

    public static async Task AddSessionMemberAsync(
        TestApiFactory factory, Guid sessionId, Guid clientId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();
        db.SessionMembers.Add(new SessionMember { SessionId = sessionId, ClientId = clientId });
        await db.SaveChangesAsync();
    }

    public static async Task<Guid> SeedSessionAsync(
        TestApiFactory factory, Guid ownerTrainerId, string title)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainerDbContext>();

        // Sessions need a TrainingType — reuse-or-create a generic one named after the test.
        var type = await db.TrainingTypes.FirstOrDefaultAsync(t => t.Name == "Test type");
        if (type is null)
        {
            type = new TrainingType { Name = "Test type", SortOrder = 1 };
            db.TrainingTypes.Add(type);
            await db.SaveChangesAsync();
        }

        var session = new Session
        {
            Title = title,
            TrainingTypeId = type.Id,
            OwnerTrainerId = ownerTrainerId,
            IsActive = true,
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    public static string IssueAccessToken(TestApiFactory factory, User user)
    {
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<JwtService>();
        return jwt.IssueAccessToken(user).Token;
    }
}
