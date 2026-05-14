using Microsoft.EntityFrameworkCore;
using Trainer.Core.Entities;

namespace Trainer.Data;

public class TrainerDbContext : DbContext
{
    public TrainerDbContext(DbContextOptions<TrainerDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.TrainingType);
            e.HasIndex(x => x.IsActive);
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
