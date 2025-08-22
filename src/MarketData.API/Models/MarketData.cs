using Shared.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace MarketData.API.Models;

public class Stock : BaseEntity
{
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Exchange { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Sector { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;
    
    public long SharesOutstanding { get; set; }
    
    public decimal MarketCap { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public string? Description { get; set; }
    
    public string? Website { get; set; }
    
    // Navigation properties
    public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public virtual ICollection<HistoricalPrice> HistoricalPrices { get; set; } = new List<HistoricalPrice>();
}

public class Quote : BaseEntity
{
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
    
    public decimal Bid { get; set; }
    
    public decimal Ask { get; set; }
    
    public decimal Open { get; set; }
    
    public decimal High { get; set; }
    
    public decimal Low { get; set; }
    
    public decimal PreviousClose { get; set; }
    
    public decimal Change { get; set; }
    
    public decimal ChangePercent { get; set; }
    
    public long Volume { get; set; }
    
    public long AverageVolume { get; set; }
    
    public decimal? PE { get; set; }
    
    public decimal? EPS { get; set; }
    
    public decimal? DividendYield { get; set; }
    
    public decimal? Week52High { get; set; }
    
    public decimal? Week52Low { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public bool IsAfterHours { get; set; } = false;
    
    // Navigation properties
    public virtual Stock? Stock { get; set; }
}

public class HistoricalPrice : BaseEntity
{
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    public decimal Open { get; set; }
    
    [Required]
    public decimal High { get; set; }
    
    [Required]
    public decimal Low { get; set; }
    
    [Required]
    public decimal Close { get; set; }
    
    public decimal AdjustedClose { get; set; }
    
    public long Volume { get; set; }
    
    public decimal? DividendAmount { get; set; }
    
    public decimal? SplitCoefficient { get; set; }
    
    // Navigation properties
    public virtual Stock? Stock { get; set; }
}

public class MarketNews : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Source { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Url { get; set; }
    
    public DateTime PublishedAt { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string? Author { get; set; }
    
    public NewsCategory Category { get; set; }
    
    public NewsSentiment Sentiment { get; set; } = NewsSentiment.Neutral;
    
    public decimal SentimentScore { get; set; } = 0;
    
    // Related symbols (comma-separated)
    public string? RelatedSymbols { get; set; }
    
    public int ViewCount { get; set; } = 0;
}

public class MarketIndex : BaseEntity
{
    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public decimal Value { get; set; }
    
    public decimal Change { get; set; }
    
    public decimal ChangePercent { get; set; }
    
    public decimal Open { get; set; }
    
    public decimal High { get; set; }
    
    public decimal Low { get; set; }
    
    public decimal PreviousClose { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class EconomicIndicator : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public decimal Value { get; set; }
    
    public decimal? PreviousValue { get; set; }
    
    public string? Unit { get; set; }
    
    public DateTime ReleaseDate { get; set; }
    
    public DateTime? NextReleaseDate { get; set; }
    
    public string? Source { get; set; }
    
    public IndicatorFrequency Frequency { get; set; }
    
    public string? Description { get; set; }
}

public enum NewsCategory
{
    General,
    Earnings,
    Mergers,
    IPO,
    Regulatory,
    Technology,
    Healthcare,
    Energy,
    Financial,
    Consumer,
    Industrial
}

public enum NewsSentiment
{
    VeryNegative = -2,
    Negative = -1,
    Neutral = 0,
    Positive = 1,
    VeryPositive = 2
}

public enum IndicatorFrequency
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Annually
}
