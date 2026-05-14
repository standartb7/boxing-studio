using Microsoft.EntityFrameworkCore;
using Trainer.Core.Entities;

namespace Trainer.Data;

public class TrainerDbContext : DbContext
{
    public TrainerDbContext(DbContextOptions<TrainerDbContext> options) : base(options) { }

    public DbSet<TrainingType> TrainingTypes => Set<TrainingType>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<TrainingType>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            // case-insensitive уникальность через NOCASE collation (SQLite)
            e.Property(x => x.Name).UseCollation("NOCASE");
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Client>(e =>
        {
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.TrainingTypeId);

            e.HasOne(x => x.TrainingType)
                .WithMany()
                .HasForeignKey(x => x.TrainingTypeId)
                .OnDelete(DeleteBehavior.Restrict); // нельзя удалить тип с клиентами

            e.HasMany(x => x.Schedule)
                .WithOne(x => x.Client)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade); // удалили клиента — слоты ушли
        });

        b.Entity<ScheduleSlot>(e =>
        {
            e.HasIndex(x => x.ClientId);
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
