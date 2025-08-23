using MarketData.API.DTOs;
using MarketData.API.Models;

namespace MarketData.API.Services;

public class MockExternalDataProvider : IExternalDataProvider
{
    private readonly Random _random;
    private readonly Dictionary<string, decimal> _basePrices;

    public MockExternalDataProvider()
    {
        _random = new Random();
        _basePrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 175.50m,
            ["GOOGL"] = 2750.80m,
            ["MSFT"] = 415.25m,
            ["AMZN"] = 145.90m,
            ["TSLA"] = 248.50m,
            ["NVDA"] = 875.20m,
            ["META"] = 485.75m,
            ["NFLX"] = 425.60m,
            ["AMD"] = 142.85m,
            ["INTC"] = 35.20m
        };
    }

    public Task<Quote?> GetQuoteAsync(string symbol)
    {
        if (!_basePrices.TryGetValue(symbol.ToUpper(), out var basePrice))
        {
            return Task.FromResult<Quote?>(null);
        }

        // Add some randomness to simulate real market movement
        var priceVariation = (decimal)(_random.NextDouble() * 0.04 - 0.02); // ±2%
        var currentPrice = Math.Round(basePrice * (1 + priceVariation), 2);
        var previousClose = Math.Round(basePrice, 2);
        var change = currentPrice - previousClose;
        var changePercent = Math.Round((change / previousClose) * 100, 2);

        var quote = new Quote
        {
            Symbol = symbol.ToUpper(),
            Price = currentPrice,
            Bid = Math.Round(currentPrice * 0.999m, 2),
            Ask = Math.Round(currentPrice * 1.001m, 2),
            Open = Math.Round(previousClose * (1 + (decimal)(_random.NextDouble() * 0.01) - 0.005m), 2),
            High = Math.Round(currentPrice * (1 + (decimal)(_random.NextDouble() * 0.02)), 2),
            Low = Math.Round(currentPrice * (1 - (decimal)(_random.NextDouble() * 0.02)), 2),
            PreviousClose = previousClose,
            Change = change,
            ChangePercent = changePercent,
            Volume = _random.Next(1000000, 100000000),
            AverageVolume = _random.Next(5000000, 50000000),
            PE = (decimal)(_random.NextDouble() * 30 + 10),
            EPS = Math.Round(currentPrice / (decimal)(_random.NextDouble() * 30 + 10), 2),
            DividendYield = (decimal)(_random.NextDouble() * 5),
            Week52High = Math.Round(currentPrice * (1 + (decimal)(_random.NextDouble() * 0.5)), 2),
            Week52Low = Math.Round(currentPrice * (1 - (decimal)(_random.NextDouble() * 0.3)), 2),
            Timestamp = DateTime.UtcNow,
            IsAfterHours = DateTime.UtcNow.Hour < 9 || DateTime.UtcNow.Hour >= 16
        };

        return Task.FromResult<Quote?>(quote);
    }

    public Task<List<HistoricalPrice>> GetHistoricalDataAsync(HistoricalDataRequestDto request)
    {
        if (!_basePrices.TryGetValue(request.Symbol.ToUpper(), out var basePrice))
        {
            return Task.FromResult(new List<HistoricalPrice>());
        }

        var historicalData = new List<HistoricalPrice>();
        var currentDate = request.StartDate;
        var currentPrice = basePrice;

        while (currentDate <= request.EndDate)
        {
            // Skip weekends for daily data
            if (request.Interval == "1d" && (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
            {
                currentDate = currentDate.AddDays(1);
                continue;
            }

            // Generate realistic price movement
            var dailyChange = (decimal)(_random.NextDouble() * 0.06 - 0.03); // ±3% daily movement
            var open = Math.Round(currentPrice, 2);
            var close = Math.Round(currentPrice * (1 + dailyChange), 2);
            var high = Math.Round(Math.Max(open, close) * (1 + (decimal)(_random.NextDouble() * 0.02)), 2);
            var low = Math.Round(Math.Min(open, close) * (1 - (decimal)(_random.NextDouble() * 0.02)), 2);

            historicalData.Add(new HistoricalPrice
            {
                Symbol = request.Symbol.ToUpper(),
                Date = currentDate,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                AdjustedClose = close,
                Volume = _random.Next(1000000, 50000000),
                CreatedAt = DateTime.UtcNow
            });

            currentPrice = close;
            currentDate = request.Interval switch
            {
                "1m" => currentDate.AddMinutes(1),
                "5m" => currentDate.AddMinutes(5),
                "15m" => currentDate.AddMinutes(15),
                "30m" => currentDate.AddMinutes(30),
                "1h" => currentDate.AddHours(1),
                "1d" => currentDate.AddDays(1),
                "1wk" => currentDate.AddDays(7),
                "1mo" => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };
        }

        return Task.FromResult(historicalData);
    }

    public Task<List<MarketNews>> GetNewsAsync(NewsFilterDto filter)
    {
        var sampleNews = new List<MarketNews>
        {
            new MarketNews
            {
                Title = "Tech Stocks Rally on Strong Earnings Reports",
                Content = "Major technology companies reported better-than-expected earnings, driving a broad rally in tech stocks...",
                Source = "Financial Times",
                Url = "https://example.com/news/1",
                PublishedAt = DateTime.UtcNow.AddHours(-2),
                Author = "John Smith",
                Category = NewsCategory.Earnings,
                Sentiment = NewsSentiment.Positive,
                SentimentScore = 0.75m,
                RelatedSymbols = "AAPL,GOOGL,MSFT",
                CreatedAt = DateTime.UtcNow
            },
            new MarketNews
            {
                Title = "Federal Reserve Signals Potential Rate Changes",
                Content = "The Federal Reserve indicated possible changes to interest rates in response to economic indicators...",
                Source = "Reuters",
                Url = "https://example.com/news/2",
                PublishedAt = DateTime.UtcNow.AddHours(-4),
                Author = "Jane Doe",
                Category = NewsCategory.Financial,
                Sentiment = NewsSentiment.Neutral,
                SentimentScore = 0.0m,
                RelatedSymbols = "SPY,QQQ",
                CreatedAt = DateTime.UtcNow
            },
            new MarketNews
            {
                Title = "Electric Vehicle Sales Show Strong Growth",
                Content = "Electric vehicle manufacturers reported record sales numbers for the quarter...",
                Source = "Bloomberg",
                Url = "https://example.com/news/3",
                PublishedAt = DateTime.UtcNow.AddHours(-6),
                Author = "Mike Johnson",
                Category = NewsCategory.General,
                Sentiment = NewsSentiment.Positive,
                SentimentScore = 0.65m,
                RelatedSymbols = "TSLA,NIO,RIVN",
                CreatedAt = DateTime.UtcNow
            },
            new MarketNews
            {
                Title = "Healthcare Sector Faces Regulatory Challenges",
                Content = "New regulations may impact healthcare companies' profitability in the coming quarters...",
                Source = "Wall Street Journal",
                Url = "https://example.com/news/4",
                PublishedAt = DateTime.UtcNow.AddHours(-8),
                Author = "Sarah Wilson",
                Category = NewsCategory.Healthcare,
                Sentiment = NewsSentiment.Negative,
                SentimentScore = -0.45m,
                RelatedSymbols = "JNJ,PFE,UNH",
                CreatedAt = DateTime.UtcNow
            },
            new MarketNews
            {
                Title = "Merger Activity Increases in Technology Sector",
                Content = "Several major technology mergers and acquisitions were announced this week...",
                Source = "CNBC",
                Url = "https://example.com/news/5",
                PublishedAt = DateTime.UtcNow.AddHours(-12),
                Author = "David Brown",
                Category = NewsCategory.Mergers,
                Sentiment = NewsSentiment.Positive,
                SentimentScore = 0.55m,
                RelatedSymbols = "MSFT,GOOGL,META",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Apply filters
        var filteredNews = sampleNews.AsQueryable();

        if (filter.Category.HasValue)
        {
            filteredNews = filteredNews.Where(n => n.Category == filter.Category.Value);
        }

        if (filter.Sentiment.HasValue)
        {
            filteredNews = filteredNews.Where(n => n.Sentiment == filter.Sentiment.Value);
        }

        if (!string.IsNullOrEmpty(filter.Symbol))
        {
            filteredNews = filteredNews.Where(n => n.RelatedSymbols != null && 
                                                 n.RelatedSymbols.Contains(filter.Symbol.ToUpper()));
        }

        if (!string.IsNullOrEmpty(filter.Source))
        {
            filteredNews = filteredNews.Where(n => n.Source.Contains(filter.Source));
        }

        if (filter.FromDate.HasValue)
        {
            filteredNews = filteredNews.Where(n => n.PublishedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            filteredNews = filteredNews.Where(n => n.PublishedAt <= filter.ToDate.Value);
        }

        return Task.FromResult(filteredNews
            .OrderByDescending(n => n.PublishedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList());
    }
}
