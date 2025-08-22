using Microsoft.EntityFrameworkCore;
using TradingEngine.API.Models;

namespace TradingEngine.API.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Trade> Trades { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Watchlist> Watchlists { get; set; }
    public DbSet<WatchlistItem> WatchlistItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order entity configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Symbol });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.StopPrice).HasPrecision(18, 8);
            entity.Property(e => e.FilledQuantity).HasPrecision(18, 8);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 8);
        });

        // Trade entity configuration
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Symbol });
            entity.HasIndex(e => e.ExecutedAt);
            
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Commission).HasPrecision(18, 8);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 8);

            entity.HasOne(t => t.Order)
                .WithMany(o => o.Trades)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Position entity configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Symbol }).IsUnique();
            
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 8);
            entity.Property(e => e.MarketValue).HasPrecision(18, 8);
            entity.Property(e => e.UnrealizedPnL).HasPrecision(18, 8);
            entity.Property(e => e.RealizedPnL).HasPrecision(18, 8);
        });

        // Watchlist entity configuration
        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // WatchlistItem entity configuration
        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasIndex(e => new { e.WatchlistId, e.Symbol }).IsUnique();
            entity.Property(e => e.AlertPrice).HasPrecision(18, 8);

            entity.HasOne(wi => wi.Watchlist)
                .WithMany(w => w.Items)
                .HasForeignKey(wi => wi.WatchlistId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
