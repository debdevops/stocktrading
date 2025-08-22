namespace Portfolio.API.Services;

public class MockMarketDataService : IMarketDataService
{
    private readonly Dictionary<string, (string CompanyName, string Sector, string Industry)> _stockInfo;
    private readonly Random _random;

    public MockMarketDataService()
    {
        _random = new Random();
        _stockInfo = new Dictionary<string, (string, string, string)>
        {
            ["AAPL"] = ("Apple Inc.", "Technology", "Consumer Electronics"),
            ["GOOGL"] = ("Alphabet Inc.", "Technology", "Internet Content & Information"),
            ["MSFT"] = ("Microsoft Corporation", "Technology", "Software—Infrastructure"),
            ["AMZN"] = ("Amazon.com, Inc.", "Consumer Cyclical", "Internet Retail"),
            ["TSLA"] = ("Tesla, Inc.", "Consumer Cyclical", "Auto Manufacturers"),
            ["NVDA"] = ("NVIDIA Corporation", "Technology", "Semiconductors"),
            ["META"] = ("Meta Platforms, Inc.", "Technology", "Internet Content & Information"),
            ["NFLX"] = ("Netflix, Inc.", "Communication Services", "Entertainment"),
            ["AMD"] = ("Advanced Micro Devices, Inc.", "Technology", "Semiconductors"),
            ["INTC"] = ("Intel Corporation", "Technology", "Semiconductors")
        };
    }

    public Task<QuoteDto?> GetQuoteAsync(string symbol)
    {
        if (!_stockInfo.TryGetValue(symbol.ToUpper(), out var stockInfo))
        {
            return Task.FromResult<QuoteDto?>(null);
        }

        // Generate mock price data
        var basePrice = symbol.ToUpper() switch
        {
            "AAPL" => 175.50m,
            "GOOGL" => 2750.80m,
            "MSFT" => 415.25m,
            "AMZN" => 145.90m,
            "TSLA" => 248.50m,
            "NVDA" => 875.20m,
            "META" => 485.75m,
            "NFLX" => 425.60m,
            "AMD" => 142.85m,
            "INTC" => 35.20m,
            _ => 100.00m
        };

        var priceVariation = (decimal)(_random.NextDouble() * 0.04 - 0.02); // ±2%
        var currentPrice = Math.Round(basePrice * (1 + priceVariation), 2);
        var dayChange = Math.Round(basePrice * (decimal)(_random.NextDouble() * 0.06 - 0.03), 2); // ±3%
        var dayChangePercent = basePrice > 0 ? Math.Round((dayChange / basePrice) * 100, 2) : 0;

        var quote = new QuoteDto
        {
            Symbol = symbol.ToUpper(),
            CompanyName = stockInfo.CompanyName,
            CurrentPrice = currentPrice,
            DayChange = dayChange,
            DayChangePercent = dayChangePercent
        };

        return Task.FromResult<QuoteDto?>(quote);
    }

    public Task<StockDto?> GetStockAsync(string symbol)
    {
        if (!_stockInfo.TryGetValue(symbol.ToUpper(), out var stockInfo))
        {
            return Task.FromResult<StockDto?>(null);
        }

        var stock = new StockDto
        {
            Symbol = symbol.ToUpper(),
            CompanyName = stockInfo.CompanyName,
            Sector = stockInfo.Sector,
            Industry = stockInfo.Industry
        };

        return Task.FromResult<StockDto?>(stock);
    }
}
