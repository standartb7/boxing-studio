using Microsoft.EntityFrameworkCore;
using Trainer.Core.Entities;

namespace Trainer.Data;

public class TrainerDbContext : DbContext
{
    public TrainerDbContext(DbContextOptions<TrainerDbContext> options) : base(options) { }

    public DbSet<TrainingType> TrainingTypes => Set<TrainingType>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionMember> SessionMembers => Set<SessionMember>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // SQLite uses NOCASE collation; Postgres uses the citext column type
        // (provider extension enabled via the initial migration). String-based provider
        // check keeps this assembly independent of Npgsql.
        var providerName = Database.ProviderName ?? string.Empty;
        var isSqlite = providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        b.Entity<TrainingType>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            if (isSqlite) e.Property(x => x.Name).UseCollation("NOCASE");
            if (isPostgres) e.Property(x => x.Name).HasColumnType("citext");
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Client>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            if (isSqlite) e.Property(x => x.Name).UseCollation("NOCASE");
            if (isPostgres) e.Property(x => x.Name).HasColumnType("citext");
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Session>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.TrainingTypeId);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.OwnerTrainerId);

            // OwnerDisplayName is a transport-only field — populated by mobile mapping
            // from SessionDto. Server reconstructs it via LEFT JOIN at query time.
            e.Ignore(x => x.OwnerDisplayName);

            e.HasOne(x => x.TrainingType)
                .WithMany()
                .HasForeignKey(x => x.TrainingTypeId)
                .OnDelete(DeleteBehavior.Restrict); // нельзя удалить тип, у которого есть сессии

            e.HasMany(x => x.Schedule)
                .WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-many Session ↔ Client через явный SessionMember.
            e.HasMany(x => x.Members)
                .WithMany(c => c.Sessions)
                .UsingEntity<SessionMember>(
                    join => join
                        .HasOne(sm => sm.Client)
                        .WithMany()
                        .HasForeignKey(sm => sm.ClientId)
                        .OnDelete(DeleteBehavior.Cascade),
                    join => join
                        .HasOne(sm => sm.Session)
                        .WithMany()
                        .HasForeignKey(sm => sm.SessionId)
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasIndex(sm => sm.SessionId);
                        join.HasIndex(sm => sm.ClientId);
                        join.HasIndex(sm => new { sm.SessionId, sm.ClientId }).IsUnique();
                    });
        });

        b.Entity<ScheduleSlot>(e =>
        {
            e.HasIndex(x => x.SessionId);
        });

        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            if (isSqlite) e.Property(x => x.Email).UseCollation("NOCASE");
            if (isPostgres) e.Property(x => x.Email).HasColumnType("citext");
            e.HasIndex(x => x.Email).IsUnique();

            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);

            e.Property(x => x.InviteCode).HasMaxLength(16);
            e.HasIndex(x => x.InviteCode).IsUnique();
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.TokenHash).IsRequired().HasMaxLength(512);
            e.Property(x => x.DeviceId).IsRequired().HasMaxLength(128);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);

            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(b);
    }

    public override int SaveChanges()
    {
        Stamp();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        Stamp();
        return base.SaveChangesAsync(ct);
    }

    private void Stamp()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
