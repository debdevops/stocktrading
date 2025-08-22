namespace TradingEngine.API.Services;

public class MockMarketDataService : IMarketDataService
{
    private readonly Dictionary<string, MarketQuote> _mockData;
    private readonly Random _random;

    public MockMarketDataService()
    {
        _random = new Random();
        _mockData = InitializeMockData();
    }

    public Task<MarketQuote?> GetQuoteAsync(string symbol)
    {
        if (_mockData.TryGetValue(symbol.ToUpper(), out var quote))
        {
            // Add some randomness to simulate real market movement
            var priceVariation = (decimal)(_random.NextDouble() * 0.02 - 0.01); // ±1%
            var newPrice = quote.Price * (1 + priceVariation);
            
            var updatedQuote = new MarketQuote
            {
                Symbol = quote.Symbol,
                Price = Math.Round(newPrice, 2),
                Bid = Math.Round(newPrice * 0.999m, 2),
                Ask = Math.Round(newPrice * 1.001m, 2),
                Change = Math.Round(newPrice - quote.Price, 2),
                ChangePercent = Math.Round(((newPrice - quote.Price) / quote.Price) * 100, 2),
                Volume = quote.Volume + _random.Next(1000, 10000),
                Timestamp = DateTime.UtcNow
            };

            return Task.FromResult<MarketQuote?>(updatedQuote);
        }

        return Task.FromResult<MarketQuote?>(null);
    }

    private Dictionary<string, MarketQuote> InitializeMockData()
    {
        return new Dictionary<string, MarketQuote>
        {
            ["AAPL"] = new MarketQuote
            {
                Symbol = "AAPL",
                Price = 175.50m,
                Bid = 175.25m,
                Ask = 175.75m,
                Change = 2.30m,
                ChangePercent = 1.33m,
                Volume = 45000000,
                Timestamp = DateTime.UtcNow
            },
            ["GOOGL"] = new MarketQuote
            {
                Symbol = "GOOGL",
                Price = 2750.80m,
                Bid = 2750.00m,
                Ask = 2751.60m,
                Change = -15.20m,
                ChangePercent = -0.55m,
                Volume = 1200000,
                Timestamp = DateTime.UtcNow
            },
            ["MSFT"] = new MarketQuote
            {
                Symbol = "MSFT",
                Price = 415.25m,
                Bid = 415.00m,
                Ask = 415.50m,
                Change = 8.75m,
                ChangePercent = 2.15m,
                Volume = 28000000,
                Timestamp = DateTime.UtcNow
            },
            ["AMZN"] = new MarketQuote
            {
                Symbol = "AMZN",
                Price = 145.90m,
                Bid = 145.75m,
                Ask = 146.05m,
                Change = -2.10m,
                ChangePercent = -1.42m,
                Volume = 35000000,
                Timestamp = DateTime.UtcNow
            },
            ["TSLA"] = new MarketQuote
            {
                Symbol = "TSLA",
                Price = 248.50m,
                Bid = 248.25m,
                Ask = 248.75m,
                Change = 12.30m,
                ChangePercent = 5.21m,
                Volume = 85000000,
                Timestamp = DateTime.UtcNow
            },
            ["NVDA"] = new MarketQuote
            {
                Symbol = "NVDA",
                Price = 875.20m,
                Bid = 874.80m,
                Ask = 875.60m,
                Change = 25.40m,
                ChangePercent = 2.99m,
                Volume = 42000000,
                Timestamp = DateTime.UtcNow
            },
            ["META"] = new MarketQuote
            {
                Symbol = "META",
                Price = 485.75m,
                Bid = 485.50m,
                Ask = 486.00m,
                Change = -8.25m,
                ChangePercent = -1.67m,
                Volume = 18000000,
                Timestamp = DateTime.UtcNow
            },
            ["NFLX"] = new MarketQuote
            {
                Symbol = "NFLX",
                Price = 425.60m,
                Bid = 425.30m,
                Ask = 425.90m,
                Change = 6.80m,
                ChangePercent = 1.62m,
                Volume = 8500000,
                Timestamp = DateTime.UtcNow
            },
            ["AMD"] = new MarketQuote
            {
                Symbol = "AMD",
                Price = 142.85m,
                Bid = 142.70m,
                Ask = 143.00m,
                Change = 3.45m,
                ChangePercent = 2.47m,
                Volume = 32000000,
                Timestamp = DateTime.UtcNow
            },
            ["INTC"] = new MarketQuote
            {
                Symbol = "INTC",
                Price = 35.20m,
                Bid = 35.15m,
                Ask = 35.25m,
                Change = -0.80m,
                ChangePercent = -2.22m,
                Volume = 28000000,
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
