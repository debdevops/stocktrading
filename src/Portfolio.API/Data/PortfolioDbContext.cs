using Microsoft.EntityFrameworkCore;
using Portfolio.API.Models;

namespace Portfolio.API.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    public DbSet<Portfolio.API.Models.Portfolio> Portfolios { get; set; }
    public DbSet<Holding> Holdings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PortfolioPerformance> PortfolioPerformances { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<PortfolioAllocation> PortfolioAllocations { get; set; }
    public DbSet<DividendRecord> DividendRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Portfolio entity configuration
        modelBuilder.Entity<Portfolio.API.Models.Portfolio>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.InitialValue).HasPrecision(18, 2);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.TotalGainLossPercent).HasPrecision(18, 4);
            entity.Property(e => e.DayGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.DayGainLossPercent).HasPrecision(18, 4);
            entity.Property(e => e.CashBalance).HasPrecision(18, 2);
            entity.Property(e => e.InvestedAmount).HasPrecision(18, 2);
        });

        // Holding entity configuration
        modelBuilder.Entity<Holding>(entity =>
        {
            entity.HasIndex(e => new { e.PortfolioId, e.Symbol }).IsUnique();
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.AverageCost).HasPrecision(18, 8);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 8);
            entity.Property(e => e.MarketValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.UnrealizedGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.UnrealizedGainLossPercent).HasPrecision(18, 4);
            entity.Property(e => e.RealizedGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.DayGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.DayGainLossPercent).HasPrecision(18, 4);

            entity.HasOne(h => h.Portfolio)
                .WithMany(p => p.Holdings)
                .HasForeignKey(h => h.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction entity configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.TransactionDate);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Commission).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(t => t.Portfolio)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PortfolioPerformance entity configuration
        modelBuilder.Entity<PortfolioPerformance>(entity =>
        {
            entity.HasIndex(e => new { e.PortfolioId, e.Date }).IsUnique();
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.Property(e => e.CashBalance).HasPrecision(18, 2);
            entity.Property(e => e.InvestedAmount).HasPrecision(18, 2);
            entity.Property(e => e.DayGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.DayGainLossPercent).HasPrecision(18, 4);
            entity.Property(e => e.TotalGainLoss).HasPrecision(18, 2);
            entity.Property(e => e.TotalGainLossPercent).HasPrecision(18, 4);

            entity.HasOne(pp => pp.Portfolio)
                .WithMany(p => p.PerformanceHistory)
                .HasForeignKey(pp => pp.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Alert entity configuration
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.TriggerValue).HasPrecision(18, 8);

            entity.HasOne(a => a.Portfolio)
                .WithMany()
                .HasForeignKey(a => a.PortfolioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PortfolioAllocation entity configuration
        modelBuilder.Entity<PortfolioAllocation>(entity =>
        {
            entity.HasIndex(e => new { e.PortfolioId, e.Category });
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            entity.Property(e => e.Value).HasPrecision(18, 2);

            entity.HasOne(pa => pa.Portfolio)
                .WithMany()
                .HasForeignKey(pa => pa.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DividendRecord entity configuration
        modelBuilder.Entity<DividendRecord>(entity =>
        {
            entity.HasIndex(e => new { e.PortfolioId, e.Symbol, e.ExDate });
            entity.Property(e => e.Amount).HasPrecision(18, 8);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);

            entity.HasOne(dr => dr.Portfolio)
                .WithMany()
                .HasForeignKey(dr => dr.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
