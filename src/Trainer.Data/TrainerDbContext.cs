using Microsoft.EntityFrameworkCore;
using Trainer.Core.Entities;

namespace Trainer.Data;

public class TrainerDbContext : DbContext
{
    public TrainerDbContext(DbContextOptions<TrainerDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<TemplateItem> TemplateItems => Set<TemplateItem>();
    public DbSet<ScheduledWorkout> ScheduledWorkouts => Set<ScheduledWorkout>();
    public DbSet<WorkoutItem> WorkoutItems => Set<WorkoutItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.IsActive);
        });

        b.Entity<Exercise>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Category);
        });

        b.Entity<WorkoutTemplate>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasMany(x => x.Items)
                .WithOne(x => x.Template)
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TemplateItem>(e =>
        {
            e.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ScheduledWorkout>(e =>
        {
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

            e.HasIndex(x => x.StartAt);
        });

        b.Entity<WorkoutItem>(e =>
        {
            e.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(b);
    }

    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(ct);
    }

    private void TouchTimestamps()
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
