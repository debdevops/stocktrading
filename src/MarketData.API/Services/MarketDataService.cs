using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MarketData.API.Data;
using MarketData.API.DTOs;
using MarketData.API.Models;

namespace MarketData.API.Services;

public interface IMarketDataService
{
    Task<QuoteDto?> GetQuoteAsync(string symbol);
    Task<List<QuoteDto>> GetQuotesAsync(List<string> symbols);
    Task<StockDto?> GetStockAsync(string symbol);
    Task<List<StockDto>> SearchStocksAsync(string query);
    Task<List<StockDto>> GetStocksAsync(MarketDataFilterDto filter);
    Task<List<HistoricalPriceDto>> GetHistoricalDataAsync(HistoricalDataRequestDto request);
    Task<ChartDataDto> GetChartDataAsync(string symbol, string interval, int period);
    Task<MarketOverviewDto> GetMarketOverviewAsync();
    Task<List<MarketIndexDto>> GetMarketIndicesAsync();
    Task<List<MarketNewsDto>> GetMarketNewsAsync(NewsFilterDto filter);
    Task<TechnicalIndicatorsDto?> GetTechnicalIndicatorsAsync(string symbol);
    Task<List<StockDto>> ScanMarketAsync(MarketScannerDto scanner);
}

public class MarketDataService : IMarketDataService
{
    private readonly MarketDataDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<MarketDataService> _logger;
    private readonly IExternalDataProvider _externalDataProvider;

    public MarketDataService(
        MarketDataDbContext context,
        IMapper mapper,
        ILogger<MarketDataService> logger,
        IExternalDataProvider externalDataProvider)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _externalDataProvider = externalDataProvider;
    }

    public async Task<QuoteDto?> GetQuoteAsync(string symbol)
    {
        try
        {
            // First try to get from database (cached data)
            var quote = await _context.Quotes
                .Where(q => q.Symbol == symbol.ToUpper())
                .OrderByDescending(q => q.Timestamp)
                .FirstOrDefaultAsync();

            // If no recent data or data is older than 1 minute, fetch from external provider
            if (quote == null || quote.Timestamp < DateTime.UtcNow.AddMinutes(-1))
            {
                var externalQuote = await _externalDataProvider.GetQuoteAsync(symbol);
                if (externalQuote != null)
                {
                    quote = await UpdateQuoteInDatabase(externalQuote);
                }
            }

            return quote != null ? _mapper.Map<QuoteDto>(quote) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote for symbol {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<QuoteDto>> GetQuotesAsync(List<string> symbols)
    {
        var quotes = new List<QuoteDto>();
        
        foreach (var symbol in symbols)
        {
            var quote = await GetQuoteAsync(symbol);
            if (quote != null)
            {
                quotes.Add(quote);
            }
        }

        return quotes;
    }

    public async Task<StockDto?> GetStockAsync(string symbol)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Symbol == symbol.ToUpper());

        if (stock == null)
        {
            return null;
        }

        var stockDto = _mapper.Map<StockDto>(stock);
        stockDto.CurrentQuote = await GetQuoteAsync(symbol);

        return stockDto;
    }

    public async Task<List<StockDto>> SearchStocksAsync(string query)
    {
        var stocks = await _context.Stocks
            .Where(s => s.Symbol.Contains(query.ToUpper()) || 
                       s.CompanyName.Contains(query))
            .Take(20)
            .ToListAsync();

        var stockDtos = _mapper.Map<List<StockDto>>(stocks);

        // Add current quotes
        foreach (var stockDto in stockDtos)
        {
            stockDto.CurrentQuote = await GetQuoteAsync(stockDto.Symbol);
        }

        return stockDtos;
    }

    public async Task<List<StockDto>> GetStocksAsync(MarketDataFilterDto filter)
    {
        var query = _context.Stocks.AsQueryable();

        if (!string.IsNullOrEmpty(filter.Symbol))
        {
            query = query.Where(s => s.Symbol.Contains(filter.Symbol.ToUpper()));
        }

        if (!string.IsNullOrEmpty(filter.Sector))
        {
            query = query.Where(s => s.Sector == filter.Sector);
        }

        if (!string.IsNullOrEmpty(filter.Exchange))
        {
            query = query.Where(s => s.Exchange == filter.Exchange);
        }

        if (filter.MinMarketCap.HasValue)
        {
            query = query.Where(s => s.MarketCap >= filter.MinMarketCap.Value);
        }

        if (filter.MaxMarketCap.HasValue)
        {
            query = query.Where(s => s.MarketCap <= filter.MaxMarketCap.Value);
        }

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortOrder == "desc" 
                ? query.OrderByDescending(s => s.CompanyName)
                : query.OrderBy(s => s.CompanyName),
            "marketcap" => filter.SortOrder == "desc"
                ? query.OrderByDescending(s => s.MarketCap)
                : query.OrderBy(s => s.MarketCap),
            _ => filter.SortOrder == "desc"
                ? query.OrderByDescending(s => s.Symbol)
                : query.OrderBy(s => s.Symbol)
        };

        var stocks = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var stockDtos = _mapper.Map<List<StockDto>>(stocks);

        // Add current quotes for price filtering
        foreach (var stockDto in stockDtos)
        {
            stockDto.CurrentQuote = await GetQuoteAsync(stockDto.Symbol);
        }

        // Apply price filtering after getting quotes
        if (filter.MinPrice.HasValue)
        {
            stockDtos = stockDtos.Where(s => s.CurrentQuote?.Price >= filter.MinPrice.Value).ToList();
        }

        if (filter.MaxPrice.HasValue)
        {
            stockDtos = stockDtos.Where(s => s.CurrentQuote?.Price <= filter.MaxPrice.Value).ToList();
        }

        if (filter.MinVolume.HasValue)
        {
            stockDtos = stockDtos.Where(s => s.CurrentQuote?.Volume >= filter.MinVolume.Value).ToList();
        }

        return stockDtos;
    }

    public async Task<List<HistoricalPriceDto>> GetHistoricalDataAsync(HistoricalDataRequestDto request)
    {
        var historicalData = await _context.HistoricalPrices
            .Where(hp => hp.Symbol == request.Symbol.ToUpper() &&
                        hp.Date >= request.StartDate &&
                        hp.Date <= request.EndDate)
            .OrderBy(hp => hp.Date)
            .ToListAsync();

        // If no data in database, try to fetch from external provider
        if (!historicalData.Any())
        {
            var externalData = await _externalDataProvider.GetHistoricalDataAsync(request);
            if (externalData.Any())
            {
                await SaveHistoricalDataToDatabase(externalData);
                historicalData = externalData;
            }
        }

        return _mapper.Map<List<HistoricalPriceDto>>(historicalData);
    }

    public async Task<ChartDataDto> GetChartDataAsync(string symbol, string interval, int period)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = interval switch
        {
            "1m" => endDate.AddDays(-1),
            "5m" => endDate.AddDays(-5),
            "15m" => endDate.AddDays(-15),
            "30m" => endDate.AddDays(-30),
            "1h" => endDate.AddDays(-60),
            "1d" => endDate.AddDays(-period),
            "1wk" => endDate.AddDays(-period * 7),
            "1mo" => endDate.AddMonths(-period),
            _ => endDate.AddDays(-30)
        };

        var request = new HistoricalDataRequestDto
        {
            Symbol = symbol,
            StartDate = startDate,
            EndDate = endDate,
            Interval = interval
        };

        var historicalData = await GetHistoricalDataAsync(request);

        return new ChartDataDto
        {
            Symbol = symbol.ToUpper(),
            Interval = interval,
            Data = historicalData.Select(h => new CandlestickDto
            {
                Timestamp = h.Date,
                Open = h.Open,
                High = h.High,
                Low = h.Low,
                Close = h.Close,
                Volume = h.Volume
            }).ToList()
        };
    }

    public async Task<MarketOverviewDto> GetMarketOverviewAsync()
    {
        var overview = new MarketOverviewDto();

        // Get market indices
        overview.Indices = await GetMarketIndicesAsync();

        // Get top gainers, losers, and most active
        var allQuotes = await _context.Quotes
            .Where(q => q.Timestamp > DateTime.UtcNow.AddHours(-1))
            .GroupBy(q => q.Symbol)
            .Select(g => g.OrderByDescending(q => q.Timestamp).First())
            .ToListAsync();

        var stocks = await _context.Stocks
            .Where(s => allQuotes.Select(q => q.Symbol).Contains(s.Symbol))
            .ToListAsync();

        var stocksWithQuotes = stocks.Select(s => new StockDto
        {
            Id = s.Id,
            Symbol = s.Symbol,
            CompanyName = s.CompanyName,
            Exchange = s.Exchange,
            Sector = s.Sector,
            Industry = s.Industry,
            SharesOutstanding = s.SharesOutstanding,
            MarketCap = s.MarketCap,
            IsActive = s.IsActive,
            Description = s.Description,
            Website = s.Website,
            CurrentQuote = _mapper.Map<QuoteDto>(allQuotes.FirstOrDefault(q => q.Symbol == s.Symbol))
        }).Where(s => s.CurrentQuote != null).ToList();

        overview.TopGainers = stocksWithQuotes
            .OrderByDescending(s => s.CurrentQuote!.ChangePercent)
            .Take(10)
            .ToList();

        overview.TopLosers = stocksWithQuotes
            .OrderBy(s => s.CurrentQuote!.ChangePercent)
            .Take(10)
            .ToList();

        overview.MostActive = stocksWithQuotes
            .OrderByDescending(s => s.CurrentQuote!.Volume)
            .Take(10)
            .ToList();

        // Get latest news
        overview.LatestNews = await GetMarketNewsAsync(new NewsFilterDto { PageSize = 10 });

        return overview;
    }

    public async Task<List<MarketIndexDto>> GetMarketIndicesAsync()
    {
        var indices = await _context.MarketIndices
            .OrderByDescending(i => i.Timestamp)
            .Take(10)
            .ToListAsync();

        return _mapper.Map<List<MarketIndexDto>>(indices);
    }

    public async Task<List<MarketNewsDto>> GetMarketNewsAsync(NewsFilterDto filter)
    {
        var query = _context.MarketNews.AsQueryable();

        if (filter.Category.HasValue)
        {
            query = query.Where(n => n.Category == filter.Category.Value);
        }

        if (filter.Sentiment.HasValue)
        {
            query = query.Where(n => n.Sentiment == filter.Sentiment.Value);
        }

        if (!string.IsNullOrEmpty(filter.Symbol))
        {
            query = query.Where(n => n.RelatedSymbols != null && 
                                   n.RelatedSymbols.Contains(filter.Symbol.ToUpper()));
        }

        if (!string.IsNullOrEmpty(filter.Source))
        {
            query = query.Where(n => n.Source.Contains(filter.Source));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(n => n.PublishedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(n => n.PublishedAt <= filter.ToDate.Value);
        }

        var news = await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return news.Select(n => new MarketNewsDto
        {
            Id = n.Id,
            Title = n.Title,
            Content = n.Content,
            Source = n.Source,
            Url = n.Url,
            PublishedAt = n.PublishedAt,
            ImageUrl = n.ImageUrl,
            Author = n.Author,
            Category = n.Category,
            Sentiment = n.Sentiment,
            SentimentScore = n.SentimentScore,
            RelatedSymbols = string.IsNullOrEmpty(n.RelatedSymbols) 
                ? new List<string>() 
                : n.RelatedSymbols.Split(',').ToList(),
            ViewCount = n.ViewCount
        }).ToList();
    }

    public async Task<TechnicalIndicatorsDto?> GetTechnicalIndicatorsAsync(string symbol)
    {
        // Get recent historical data for calculations
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-200); // Need enough data for 200-day SMA

        var historicalData = await _context.HistoricalPrices
            .Where(hp => hp.Symbol == symbol.ToUpper() &&
                        hp.Date >= startDate &&
                        hp.Date <= endDate)
            .OrderBy(hp => hp.Date)
            .ToListAsync();

        if (historicalData.Count < 50)
        {
            return null; // Not enough data for calculations
        }

        // Calculate technical indicators
        var indicators = new TechnicalIndicatorsDto
        {
            Symbol = symbol.ToUpper(),
            CalculatedAt = DateTime.UtcNow
        };

        var prices = historicalData.Select(h => h.Close).ToList();
        var highs = historicalData.Select(h => h.High).ToList();
        var lows = historicalData.Select(h => h.Low).ToList();

        // Calculate RSI (14-period)
        indicators.RSI = CalculateRSI(prices, 14);

        // Calculate Moving Averages
        indicators.SMA20 = CalculateSMA(prices, 20);
        indicators.SMA50 = CalculateSMA(prices, 50);
        if (prices.Count >= 200)
        {
            indicators.SMA200 = CalculateSMA(prices, 200);
        }

        indicators.EMA12 = CalculateEMA(prices, 12);
        indicators.EMA26 = CalculateEMA(prices, 26);

        // Calculate MACD
        var macdData = CalculateMACD(prices);
        indicators.MACD = macdData.MACD;
        indicators.MACDSignal = macdData.Signal;
        indicators.MACDHistogram = macdData.Histogram;

        // Calculate Bollinger Bands
        var bollingerData = CalculateBollingerBands(prices, 20, 2);
        indicators.BollingerUpper = bollingerData.Upper;
        indicators.BollingerMiddle = bollingerData.Middle;
        indicators.BollingerLower = bollingerData.Lower;

        // Calculate Stochastic
        var stochasticData = CalculateStochastic(highs, lows, prices, 14, 3);
        indicators.StochasticK = stochasticData.K;
        indicators.StochasticD = stochasticData.D;

        return indicators;
    }

    public async Task<List<StockDto>> ScanMarketAsync(MarketScannerDto scanner)
    {
        // This is a simplified implementation
        // In a real system, you'd want more sophisticated scanning logic
        
        var allStocks = await _context.Stocks
            .Where(s => s.IsActive)
            .Take(scanner.MaxResults)
            .ToListAsync();

        var results = new List<StockDto>();

        foreach (var stock in allStocks)
        {
            var quote = await GetQuoteAsync(stock.Symbol);
            if (quote == null) continue;

            var stockDto = _mapper.Map<StockDto>(stock);
            stockDto.CurrentQuote = quote;

            // Check if stock meets all criteria
            var meetsCriteria = true;
            foreach (var criteria in scanner.Criteria)
            {
                var fieldValue = GetFieldValue(stockDto, criteria.Field);
                if (!EvaluateCriteria(fieldValue, criteria.Operator, criteria.Value))
                {
                    meetsCriteria = false;
                    break;
                }
            }

            if (meetsCriteria)
            {
                results.Add(stockDto);
            }
        }

        return results;
    }

    private async Task<Quote> UpdateQuoteInDatabase(Quote externalQuote)
    {
        _context.Quotes.Add(externalQuote);
        await _context.SaveChangesAsync();
        return externalQuote;
    }

    private async Task SaveHistoricalDataToDatabase(List<HistoricalPrice> historicalData)
    {
        _context.HistoricalPrices.AddRange(historicalData);
        await _context.SaveChangesAsync();
    }

    // Technical indicator calculation methods
    private decimal? CalculateRSI(List<decimal> prices, int period)
    {
        if (prices.Count < period + 1) return null;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < prices.Count; i++)
        {
            var change = prices[i] - prices[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        if (gains.Count < period) return null;

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100;

        var rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    private decimal? CalculateSMA(List<decimal> prices, int period)
    {
        if (prices.Count < period) return null;
        return prices.TakeLast(period).Average();
    }

    private decimal? CalculateEMA(List<decimal> prices, int period)
    {
        if (prices.Count < period) return null;

        var multiplier = 2m / (period + 1);
        var ema = prices.Take(period).Average();

        for (int i = period; i < prices.Count; i++)
        {
            ema = (prices[i] * multiplier) + (ema * (1 - multiplier));
        }

        return ema;
    }

    private (decimal? MACD, decimal? Signal, decimal? Histogram) CalculateMACD(List<decimal> prices)
    {
        var ema12 = CalculateEMA(prices, 12);
        var ema26 = CalculateEMA(prices, 26);

        if (!ema12.HasValue || !ema26.HasValue) return (null, null, null);

        var macd = ema12.Value - ema26.Value;
        
        // For signal line, we'd need to calculate EMA of MACD values
        // This is simplified - in practice, you'd maintain MACD history
        var signal = macd * 0.9m; // Simplified approximation
        var histogram = macd - signal;

        return (macd, signal, histogram);
    }

    private (decimal? Upper, decimal? Middle, decimal? Lower) CalculateBollingerBands(List<decimal> prices, int period, decimal stdDevMultiplier)
    {
        var sma = CalculateSMA(prices, period);
        if (!sma.HasValue) return (null, null, null);

        var recentPrices = prices.TakeLast(period).ToList();
        var variance = recentPrices.Sum(p => (p - sma.Value) * (p - sma.Value)) / period;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return (sma.Value + (stdDev * stdDevMultiplier), sma.Value, sma.Value - (stdDev * stdDevMultiplier));
    }

    private (decimal? K, decimal? D) CalculateStochastic(List<decimal> highs, List<decimal> lows, List<decimal> closes, int kPeriod, int dPeriod)
    {
        if (highs.Count < kPeriod) return (null, null);

        var recentHighs = highs.TakeLast(kPeriod);
        var recentLows = lows.TakeLast(kPeriod);
        var currentClose = closes.Last();

        var highestHigh = recentHighs.Max();
        var lowestLow = recentLows.Min();

        if (highestHigh == lowestLow) return (null, null);

        var k = ((currentClose - lowestLow) / (highestHigh - lowestLow)) * 100;
        var d = k * 0.9m; // Simplified - should be SMA of %K values

        return (k, d);
    }

    private decimal? GetFieldValue(StockDto stock, string field)
    {
        return field.ToLower() switch
        {
            "price" => stock.CurrentQuote?.Price,
            "volume" => stock.CurrentQuote?.Volume,
            "change_percent" => stock.CurrentQuote?.ChangePercent,
            "market_cap" => stock.MarketCap,
            "pe" => stock.CurrentQuote?.PE,
            _ => null
        };
    }

    private bool EvaluateCriteria(decimal? fieldValue, string operatorType, decimal criteriaValue)
    {
        if (!fieldValue.HasValue) return false;

        return operatorType.ToLower() switch
        {
            "gt" => fieldValue.Value > criteriaValue,
            "gte" => fieldValue.Value >= criteriaValue,
            "lt" => fieldValue.Value < criteriaValue,
            "lte" => fieldValue.Value <= criteriaValue,
            "eq" => fieldValue.Value == criteriaValue,
            _ => false
        };
    }
}

// External data provider interface (for real market data APIs)
public interface IExternalDataProvider
{
    Task<Quote?> GetQuoteAsync(string symbol);
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(HistoricalDataRequestDto request);
    Task<List<MarketNews>> GetNewsAsync(NewsFilterDto filter);
}
