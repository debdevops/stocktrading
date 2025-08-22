using System.ComponentModel.DataAnnotations;
using MarketData.API.Models;

namespace MarketData.API.DTOs;

public class QuoteDto
{
    public string Symbol { get; set; } = string.Empty;
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
    public DateTime Timestamp { get; set; }
    public bool IsAfterHours { get; set; }
}

public class StockDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public long SharesOutstanding { get; set; }
    public decimal MarketCap { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public QuoteDto? CurrentQuote { get; set; }
}

public class HistoricalPriceDto
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal AdjustedClose { get; set; }
    public long Volume { get; set; }
    public decimal? DividendAmount { get; set; }
    public decimal? SplitCoefficient { get; set; }
}

public class ChartDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public List<CandlestickDto> Data { get; set; } = new();
}

public class CandlestickDto
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class MarketNewsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Url { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }
    public NewsCategory Category { get; set; }
    public NewsSentiment Sentiment { get; set; }
    public decimal SentimentScore { get; set; }
    public List<string> RelatedSymbols { get; set; } = new();
    public int ViewCount { get; set; }
}

public class MarketIndexDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal PreviousClose { get; set; }
    public DateTime Timestamp { get; set; }
}

public class EconomicIndicatorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? PreviousValue { get; set; }
    public string? Unit { get; set; }
    public DateTime ReleaseDate { get; set; }
    public DateTime? NextReleaseDate { get; set; }
    public string? Source { get; set; }
    public IndicatorFrequency Frequency { get; set; }
    public string? Description { get; set; }
}

public class MarketOverviewDto
{
    public List<MarketIndexDto> Indices { get; set; } = new();
    public List<StockDto> TopGainers { get; set; } = new();
    public List<StockDto> TopLosers { get; set; } = new();
    public List<StockDto> MostActive { get; set; } = new();
    public List<MarketNewsDto> LatestNews { get; set; } = new();
}

public class StockSearchDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal? CurrentPrice { get; set; }
    public decimal? ChangePercent { get; set; }
}

public class MarketDataFilterDto
{
    public string? Symbol { get; set; }
    public string? Sector { get; set; }
    public string? Exchange { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinVolume { get; set; }
    public decimal? MinMarketCap { get; set; }
    public decimal? MaxMarketCap { get; set; }
    public string? SortBy { get; set; } = "symbol";
    public string? SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class NewsFilterDto
{
    public NewsCategory? Category { get; set; }
    public NewsSentiment? Sentiment { get; set; }
    public string? Symbol { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Source { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class HistoricalDataRequestDto
{
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public string Interval { get; set; } = "1d"; // 1m, 5m, 15m, 30m, 1h, 1d, 1wk, 1mo
}

public class MarketScannerDto
{
    public List<ScannerCriteriaDto> Criteria { get; set; } = new();
    public int MaxResults { get; set; } = 100;
}

public class ScannerCriteriaDto
{
    public string Field { get; set; } = string.Empty; // price, volume, change_percent, market_cap, etc.
    public string Operator { get; set; } = string.Empty; // gt, lt, eq, gte, lte
    public decimal Value { get; set; }
}

public class TechnicalIndicatorsDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal? RSI { get; set; }
    public decimal? MACD { get; set; }
    public decimal? MACDSignal { get; set; }
    public decimal? MACDHistogram { get; set; }
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA200 { get; set; }
    public decimal? EMA12 { get; set; }
    public decimal? EMA26 { get; set; }
    public decimal? BollingerUpper { get; set; }
    public decimal? BollingerLower { get; set; }
    public decimal? BollingerMiddle { get; set; }
    public decimal? StochasticK { get; set; }
    public decimal? StochasticD { get; set; }
    public DateTime CalculatedAt { get; set; }
}
