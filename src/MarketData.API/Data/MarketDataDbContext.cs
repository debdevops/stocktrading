using Microsoft.EntityFrameworkCore;
using MarketData.API.Models;

namespace MarketData.API.Data;

public class MarketDataDbContext : DbContext
{
    public MarketDataDbContext(DbContextOptions<MarketDataDbContext> options) : base(options)
    {
    }

    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<HistoricalPrice> HistoricalPrices { get; set; }
    public DbSet<MarketNews> MarketNews { get; set; }
    public DbSet<MarketIndex> MarketIndices { get; set; }
    public DbSet<EconomicIndicator> EconomicIndicators { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Stock entity configuration
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(e => e.Symbol).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MarketCap).HasPrecision(18, 2);
        });

        // Quote entity configuration
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.Timestamp);
            
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Bid).HasPrecision(18, 8);
            entity.Property(e => e.Ask).HasPrecision(18, 8);
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.PreviousClose).HasPrecision(18, 8);
            entity.Property(e => e.Change).HasPrecision(18, 8);
            entity.Property(e => e.ChangePercent).HasPrecision(18, 4);
            entity.Property(e => e.PE).HasPrecision(18, 4);
            entity.Property(e => e.EPS).HasPrecision(18, 4);
            entity.Property(e => e.DividendYield).HasPrecision(18, 4);
            entity.Property(e => e.Week52High).HasPrecision(18, 8);
            entity.Property(e => e.Week52Low).HasPrecision(18, 8);

            entity.HasOne(q => q.Stock)
                .WithMany(s => s.Quotes)
                .HasForeignKey(q => q.Symbol)
                .HasPrincipalKey(s => s.Symbol)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HistoricalPrice entity configuration
        modelBuilder.Entity<HistoricalPrice>(entity =>
        {
            entity.HasIndex(e => new { e.Symbol, e.Date }).IsUnique();
            entity.HasIndex(e => e.Date);
            
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.Property(e => e.AdjustedClose).HasPrecision(18, 8);
            entity.Property(e => e.DividendAmount).HasPrecision(18, 8);
            entity.Property(e => e.SplitCoefficient).HasPrecision(18, 8);

            entity.HasOne(hp => hp.Stock)
                .WithMany(s => s.HistoricalPrices)
                .HasForeignKey(hp => hp.Symbol)
                .HasPrincipalKey(s => s.Symbol)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MarketNews entity configuration
        modelBuilder.Entity<MarketNews>(entity =>
        {
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Sentiment);
            
            entity.Property(e => e.SentimentScore).HasPrecision(5, 2);
        });

        // MarketIndex entity configuration
        modelBuilder.Entity<MarketIndex>(entity =>
        {
            entity.HasIndex(e => e.Symbol).IsUnique();
            entity.HasIndex(e => e.Timestamp);
            
            entity.Property(e => e.Value).HasPrecision(18, 4);
            entity.Property(e => e.Change).HasPrecision(18, 4);
            entity.Property(e => e.ChangePercent).HasPrecision(18, 4);
            entity.Property(e => e.Open).HasPrecision(18, 4);
            entity.Property(e => e.High).HasPrecision(18, 4);
            entity.Property(e => e.Low).HasPrecision(18, 4);
            entity.Property(e => e.PreviousClose).HasPrecision(18, 4);
        });

        // EconomicIndicator entity configuration
        modelBuilder.Entity<EconomicIndicator>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ReleaseDate);
            
            entity.Property(e => e.Value).HasPrecision(18, 4);
            entity.Property(e => e.PreviousValue).HasPrecision(18, 4);
        });

        // Seed data for popular stocks
        SeedStockData(modelBuilder);
        SeedMarketIndices(modelBuilder);
    }

    private void SeedStockData(ModelBuilder modelBuilder)
    {
        var stocks = new[]
        {
            new Stock
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                CompanyName = "Apple Inc.",
                Exchange = "NASDAQ",
                Sector = "Technology",
                Industry = "Consumer Electronics",
                SharesOutstanding = 15728700000,
                MarketCap = 2800000000000m,
                IsActive = true,
                Description = "Apple Inc. designs, manufactures, and markets smartphones, personal computers, tablets, wearables, and accessories worldwide.",
                Website = "https://www.apple.com",
                CreatedAt = DateTime.UtcNow
            },
            new Stock
            {
                Id = Guid.NewGuid(),
                Symbol = "GOOGL",
                CompanyName = "Alphabet Inc.",
                Exchange = "NASDAQ",
                Sector = "Technology",
                Industry = "Internet Content & Information",
                SharesOutstanding = 12900000000,
                MarketCap = 1700000000000m,
                IsActive = true,
                Description = "Alphabet Inc. provides various products and platforms in the United States, Europe, the Middle East, Africa, the Asia-Pacific, Canada, and Latin America.",
                Website = "https://abc.xyz",
                CreatedAt = DateTime.UtcNow
            },
            new Stock
            {
                Id = Guid.NewGuid(),
                Symbol = "MSFT",
                CompanyName = "Microsoft Corporation",
                Exchange = "NASDAQ",
                Sector = "Technology",
                Industry = "Software—Infrastructure",
                SharesOutstanding = 7430000000,
                MarketCap = 3100000000000m,
                IsActive = true,
                Description = "Microsoft Corporation develops, licenses, and supports software, services, devices, and solutions worldwide.",
                Website = "https://www.microsoft.com",
                CreatedAt = DateTime.UtcNow
            },
            new Stock
            {
                Id = Guid.NewGuid(),
                Symbol = "AMZN",
                CompanyName = "Amazon.com, Inc.",
                Exchange = "NASDAQ",
                Sector = "Consumer Cyclical",
                Industry = "Internet Retail",
                SharesOutstanding = 10700000000,
                MarketCap = 1500000000000m,
                IsActive = true,
                Description = "Amazon.com, Inc. engages in the retail sale of consumer products and subscriptions in North America and internationally.",
                Website = "https://www.amazon.com",
                CreatedAt = DateTime.UtcNow
            },
            new Stock
            {
                Id = Guid.NewGuid(),
                Symbol = "TSLA",
                CompanyName = "Tesla, Inc.",
                Exchange = "NASDAQ",
                Sector = "Consumer Cyclical",
                Industry = "Auto Manufacturers",
                SharesOutstanding = 3170000000,
                MarketCap = 800000000000m,
                IsActive = true,
                Description = "Tesla, Inc. designs, develops, manufactures, leases, and sells electric vehicles, and energy generation and storage systems.",
                Website = "https://www.tesla.com",
                CreatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<Stock>().HasData(stocks);
    }

    private void SeedMarketIndices(ModelBuilder modelBuilder)
    {
        var indices = new[]
        {
            new MarketIndex
            {
                Id = Guid.NewGuid(),
                Symbol = "SPX",
                Name = "S&P 500",
                Value = 4500.25m,
                Change = 15.30m,
                ChangePercent = 0.34m,
                Open = 4485.00m,
                High = 4502.80m,
                Low = 4480.15m,
                PreviousClose = 4484.95m,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new MarketIndex
            {
                Id = Guid.NewGuid(),
                Symbol = "DJI",
                Name = "Dow Jones Industrial Average",
                Value = 35200.75m,
                Change = -125.50m,
                ChangePercent = -0.36m,
                Open = 35350.00m,
                High = 35380.25m,
                Low = 35180.50m,
                PreviousClose = 35326.25m,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new MarketIndex
            {
                Id = Guid.NewGuid(),
                Symbol = "IXIC",
                Name = "NASDAQ Composite",
                Value = 14250.80m,
                Change = 85.40m,
                ChangePercent = 0.60m,
                Open = 14180.00m,
                High = 14275.30m,
                Low = 14165.25m,
                PreviousClose = 14165.40m,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<MarketIndex>().HasData(indices);
    }
}
