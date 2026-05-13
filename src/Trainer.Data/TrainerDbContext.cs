using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data;

public class TrainerDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public TrainerDbContext(DbContextOptions<TrainerDbContext> options, ITenantContext tenant) : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<TemplateItem> TemplateItems => Set<TemplateItem>();
    public DbSet<ScheduledWorkout> ScheduledWorkouts => Set<ScheduledWorkout>();
    public DbSet<WorkoutItem> WorkoutItems => Set<WorkoutItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
            // NB: НЕТ HasQueryFilter — логин должен находить пользователя по email до того,
            // как мы узнаем его TenantId. Безопасность login-endpoint обеспечивается тем,
            // что он публичный по дизайну и проверяет пароль.
        });

        b.Entity<Client>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenant.CurrentTenantId);
        });

        b.Entity<Exercise>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenant.CurrentTenantId);
        });

        b.Entity<WorkoutTemplate>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenant.CurrentTenantId);
            e.HasMany(x => x.Items)
                .WithOne(x => x.Template)
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TemplateItem>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenant.CurrentTenantId);
            e.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ScheduledWorkout>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.StartAt);
            e.HasQueryFilter(x => x.TenantId == _tenant.CurrentTenantId);

            e.HasOne(x => x.Client)
                .WithMany(x => x.Workouts)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Template)
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(x => x.Items)
                .WithOne(x => x.ScheduledWorkout)
                .HasForeignKey(x => x.ScheduledWorkoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WorkoutItem>(e =>
        {
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenant.CurrentTenantId);
            e.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(b);
    }

    public override int SaveChanges()
    {
        StampEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        StampEntities();
        return base.SaveChangesAsync(ct);
    }

    private void StampEntities()
    {
        var now = DateTimeOffset.UtcNow;
        var tenantId = _tenant.CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (entry.Entity.TenantId == Guid.Empty)
                    entry.Entity.TenantId = tenantId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                // защита от подмены TenantId: запрещаем менять его на UPDATE
                entry.Property(nameof(EntityBase.TenantId)).IsModified = false;
            }
        }
    }
}
