using AmiyaDbaasManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AmiyaDbaasManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DbInstance> DbInstances => Set<DbInstance>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        // ── DbInstance ────────────────────────────────────────
        modelBuilder.Entity<DbInstance>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasOne(d => d.User)
                .WithMany(u => u.DbInstances)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Plan ──────────────────────────────────────────────
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(p => p.PlanId);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.PriceMonthly).HasColumnType("decimal(18,2)");
            entity.HasIndex(p => p.Name).IsUnique();
        });

        // ── UserSubscription ──────────────────────────────────
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasOne(s => s.User)
                .WithMany(u => u.UserSubscriptions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Plan)
                .WithMany(p => p.UserSubscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chỉ 1 subscription active tại 1 thời điểm mỗi user
            entity.HasIndex(s => new { s.UserId, s.IsActive })
                .HasFilter("\"IsActive\" = true");
        });
    }
}
