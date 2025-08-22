using Microsoft.EntityFrameworkCore;
using UserManagement.API.Models;

namespace UserManagement.API.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });

        // Role entity configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        });

        // UserRole entity configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserProfile entity configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            
            entity.HasOne(up => up.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default roles
        var adminRoleId = Guid.NewGuid();
        var traderRoleId = Guid.NewGuid();
        var viewerRoleId = Guid.NewGuid();

        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "System administrator with full access",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = traderRoleId,
                Name = "Trader",
                Description = "Active trader with trading permissions",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = viewerRoleId,
                Name = "Viewer",
                Description = "Read-only access to market data",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
