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

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<TrainingType>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).UseCollation("NOCASE");
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Client>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.IsActive);
        });

        b.Entity<Session>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.TrainingTypeId);
            e.HasIndex(x => x.IsActive);

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
