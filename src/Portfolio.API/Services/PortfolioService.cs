using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Portfolio.API.Data;
using Portfolio.API.DTOs;
using Portfolio.API.Models;

namespace Portfolio.API.Services;

public interface IPortfolioService
{
    Task<PortfolioDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto portfolioDto);
    Task<List<PortfolioDto>> GetPortfoliosAsync(Guid userId);
    Task<PortfolioDto> GetPortfolioAsync(Guid userId, Guid portfolioId);
    Task<PortfolioDto> UpdatePortfolioAsync(Guid userId, Guid portfolioId, CreatePortfolioDto portfolioDto);
    Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId);
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId);
    Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(Guid userId, Guid portfolioId);
    Task UpdatePortfolioValuesAsync(Guid portfolioId);
    Task<RebalanceRecommendationDto> GetRebalanceRecommendationAsync(Guid userId, Guid portfolioId);
}

public class PortfolioService : IPortfolioService
{
    private readonly PortfolioDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PortfolioService> _logger;
    private readonly IMarketDataService _marketDataService;

    public PortfolioService(
        PortfolioDbContext context,
        IMapper mapper,
        ILogger<PortfolioService> logger,
        IMarketDataService marketDataService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _marketDataService = marketDataService;
    }

    public async Task<PortfolioDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto portfolioDto)
    {
        // If this is set as default, unset other default portfolios
        if (portfolioDto.IsDefault)
        {
            var existingDefaults = await _context.Portfolios
                .Where(p => p.UserId == userId && p.IsDefault)
                .ToListAsync();
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        var portfolio = new Portfolio.API.Models.Portfolio
        {
            UserId = userId,
            Name = portfolioDto.Name,
            Description = portfolioDto.Description,
            InitialValue = portfolioDto.InitialValue,
            CurrentValue = portfolioDto.InitialValue,
            CashBalance = portfolioDto.InitialValue,
            InvestedAmount = 0,
            IsDefault = portfolioDto.IsDefault,
            LastUpdated = DateTime.UtcNow
        };

        _context.Portfolios.Add(portfolio);

        // Create initial performance record
        var initialPerformance = new PortfolioPerformance
        {
            PortfolioId = portfolio.Id,
            Date = DateTime.UtcNow.Date,
            TotalValue = portfolioDto.InitialValue,
            CashBalance = portfolioDto.InitialValue,
            InvestedAmount = 0,
            DayGainLoss = 0,
            DayGainLossPercent = 0,
            TotalGainLoss = 0,
            TotalGainLossPercent = 0
        };

        _context.PortfolioPerformances.Add(initialPerformance);
        await _context.SaveChangesAsync();

        return await GetPortfolioAsync(userId, portfolio.Id);
    }

    public async Task<List<PortfolioDto>> GetPortfoliosAsync(Guid userId)
    {
        var portfolios = await _context.Portfolios
            .Include(p => p.Holdings)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var portfolioDtos = new List<PortfolioDto>();

        foreach (var portfolio in portfolios)
        {
            await UpdatePortfolioValuesAsync(portfolio.Id);
            var portfolioDto = await GetPortfolioAsync(userId, portfolio.Id);
            portfolioDtos.Add(portfolioDto);
        }

        return portfolioDtos;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await _context.Portfolios
            .Include(p => p.Holdings)
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

        if (portfolio == null)
        {
            throw new KeyNotFoundException("Portfolio not found");
        }

        var portfolioDto = _mapper.Map<PortfolioDto>(portfolio);

        // Get holdings with current market data
        var holdingDtos = new List<HoldingDto>();
        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var holdingDto = _mapper.Map<HoldingDto>(holding);
            
            // Get company name and current price from market data service
            var marketData = await _marketDataService.GetQuoteAsync(holding.Symbol);
            if (marketData != null)
            {
                holdingDto.CompanyName = marketData.CompanyName ?? holding.Symbol;
                holdingDto.CurrentPrice = marketData.CurrentPrice;
                holdingDto.MarketValue = holding.Quantity * marketData.CurrentPrice;
                holdingDto.UnrealizedGainLoss = holdingDto.MarketValue - holding.TotalCost;
                holdingDto.UnrealizedGainLossPercent = holding.TotalCost > 0 
                    ? (holdingDto.UnrealizedGainLoss / holding.TotalCost) * 100 
                    : 0;
                holdingDto.DayGainLoss = holding.Quantity * marketData.DayChange;
                holdingDto.DayGainLossPercent = marketData.DayChangePercent;
            }

            holdingDtos.Add(holdingDto);
        }

        portfolioDto.Holdings = holdingDtos;

        // Calculate allocation percentages
        var totalValue = holdingDtos.Sum(h => h.MarketValue);
        if (totalValue > 0)
        {
            foreach (var holding in holdingDtos)
            {
                holding.AllocationPercent = (holding.MarketValue / totalValue) * 100;
            }
        }

        // Get allocations by sector
        portfolioDto.Allocations = await GetPortfolioAllocationsAsync(portfolioId);

        return portfolioDto;
    }

    public async Task<PortfolioDto> UpdatePortfolioAsync(Guid userId, Guid portfolioId, CreatePortfolioDto portfolioDto)
    {
        var portfolio = await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

        if (portfolio == null)
        {
            throw new KeyNotFoundException("Portfolio not found");
        }

        // If this is set as default, unset other default portfolios
        if (portfolioDto.IsDefault && !portfolio.IsDefault)
        {
            var existingDefaults = await _context.Portfolios
                .Where(p => p.UserId == userId && p.IsDefault && p.Id != portfolioId)
                .ToListAsync();
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        portfolio.Name = portfolioDto.Name;
        portfolio.Description = portfolioDto.Description;
        portfolio.IsDefault = portfolioDto.IsDefault;
        portfolio.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPortfolioAsync(userId, portfolioId);
    }

    public async Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

        if (portfolio == null)
        {
            throw new KeyNotFoundException("Portfolio not found");
        }

        // Check if portfolio has holdings
        var hasHoldings = await _context.Holdings
            .AnyAsync(h => h.PortfolioId == portfolioId && h.Quantity > 0);

        if (hasHoldings)
        {
            throw new InvalidOperationException("Cannot delete portfolio with active holdings");
        }

        _context.Portfolios.Remove(portfolio);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId)
    {
        var portfolios = await GetPortfoliosAsync(userId);

        var summary = new PortfolioSummaryDto
        {
            TotalPortfolios = portfolios.Count,
            TotalValue = portfolios.Sum(p => p.CurrentValue),
            TotalGainLoss = portfolios.Sum(p => p.TotalGainLoss),
            DayGainLoss = portfolios.Sum(p => p.DayGainLoss),
            CashBalance = portfolios.Sum(p => p.CashBalance),
            InvestedAmount = portfolios.Sum(p => p.InvestedAmount),
            Portfolios = portfolios
        };

        // Calculate percentages
        if (summary.InvestedAmount > 0)
        {
            summary.TotalGainLossPercent = (summary.TotalGainLoss / summary.InvestedAmount) * 100;
        }

        var previousValue = summary.TotalValue - summary.DayGainLoss;
        if (previousValue > 0)
        {
            summary.DayGainLossPercent = (summary.DayGainLoss / previousValue) * 100;
        }

        // Get top holdings across all portfolios
        var allHoldings = portfolios.SelectMany(p => p.Holdings).ToList();
        summary.TopHoldings = allHoldings
            .OrderByDescending(h => h.MarketValue)
            .Take(10)
            .ToList();

        // Get overall allocation
        summary.OverallAllocation = await GetOverallAllocationAsync(userId);

        return summary;
    }

    public async Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

        if (portfolio == null)
        {
            throw new KeyNotFoundException("Portfolio not found");
        }

        var performanceHistory = await _context.PortfolioPerformances
            .Where(pp => pp.PortfolioId == portfolioId)
            .OrderBy(pp => pp.Date)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.PortfolioId == portfolioId && 
                       (t.Type == TransactionType.Buy || t.Type == TransactionType.Sell))
            .ToListAsync();

        var analytics = new PortfolioAnalyticsDto
        {
            PortfolioId = portfolioId,
            PortfolioName = portfolio.Name,
            PerformanceHistory = _mapper.Map<List<PortfolioPerformanceDto>>(performanceHistory)
        };

        if (performanceHistory.Any())
        {
            // Calculate returns
            var initialValue = performanceHistory.First().TotalValue;
            var currentValue = performanceHistory.Last().TotalValue;
            
            analytics.TotalReturn = initialValue > 0 ? ((currentValue - initialValue) / initialValue) * 100 : 0;

            // Calculate annualized return
            var daysDiff = (performanceHistory.Last().Date - performanceHistory.First().Date).Days;
            if (daysDiff > 0)
            {
                var years = daysDiff / 365.25;
                analytics.AnnualizedReturn = years > 0 
                    ? (decimal)(Math.Pow((double)(currentValue / initialValue), 1.0 / years) - 1) * 100
                    : 0;
            }

            // Calculate volatility (standard deviation of daily returns)
            var dailyReturns = new List<decimal>();
            for (int i = 1; i < performanceHistory.Count; i++)
            {
                var prevValue = performanceHistory[i - 1].TotalValue;
                var currValue = performanceHistory[i].TotalValue;
                if (prevValue > 0)
                {
                    dailyReturns.Add((currValue - prevValue) / prevValue);
                }
            }

            if (dailyReturns.Any())
            {
                var avgReturn = dailyReturns.Average();
                var variance = dailyReturns.Sum(r => (r - avgReturn) * (r - avgReturn)) / dailyReturns.Count;
                analytics.Volatility = (decimal)Math.Sqrt((double)variance) * (decimal)Math.Sqrt(252) * 100; // Annualized
            }

            // Calculate max drawdown
            var peak = initialValue;
            var maxDrawdown = 0m;
            foreach (var performance in performanceHistory)
            {
                if (performance.TotalValue > peak)
                {
                    peak = performance.TotalValue;
                }
                var drawdown = (peak - performance.TotalValue) / peak;
                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }
            analytics.MaxDrawdown = maxDrawdown * 100;

            // Calculate Sharpe ratio (assuming 2% risk-free rate)
            var riskFreeRate = 0.02m;
            if (analytics.Volatility > 0)
            {
                analytics.SharpeRatio = (analytics.AnnualizedReturn / 100 - riskFreeRate) / (analytics.Volatility / 100);
            }
        }

        // Calculate trading statistics
        analytics.TotalTrades = transactions.Count;
        
        var winningTrades = transactions.Where(t => t.Type == TransactionType.Sell && t.TotalAmount > 0).Count();
        analytics.WinRate = analytics.TotalTrades > 0 ? (decimal)winningTrades / analytics.TotalTrades * 100 : 0;

        return analytics;
    }

    public async Task UpdatePortfolioValuesAsync(Guid portfolioId)
    {
        var portfolio = await _context.Portfolios
            .Include(p => p.Holdings)
            .FirstOrDefaultAsync(p => p.Id == portfolioId);

        if (portfolio == null) return;

        decimal totalMarketValue = portfolio.CashBalance;
        decimal totalCost = 0;
        decimal dayGainLoss = 0;

        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var marketData = await _marketDataService.GetQuoteAsync(holding.Symbol);
            if (marketData != null)
            {
                holding.CurrentPrice = marketData.CurrentPrice;
                holding.MarketValue = holding.Quantity * marketData.CurrentPrice;
                holding.UnrealizedGainLoss = holding.MarketValue - holding.TotalCost;
                holding.UnrealizedGainLossPercent = holding.TotalCost > 0 
                    ? (holding.UnrealizedGainLoss / holding.TotalCost) * 100 
                    : 0;
                holding.DayGainLoss = holding.Quantity * marketData.DayChange;
                holding.DayGainLossPercent = marketData.DayChangePercent;
                holding.LastUpdated = DateTime.UtcNow;

                totalMarketValue += holding.MarketValue;
                totalCost += holding.TotalCost;
                dayGainLoss += holding.DayGainLoss;
            }
        }

        // Update portfolio values
        portfolio.CurrentValue = totalMarketValue;
        portfolio.InvestedAmount = totalCost;
        portfolio.TotalGainLoss = totalMarketValue - portfolio.InitialValue;
        portfolio.TotalGainLossPercent = portfolio.InitialValue > 0 
            ? (portfolio.TotalGainLoss / portfolio.InitialValue) * 100 
            : 0;
        portfolio.DayGainLoss = dayGainLoss;
        
        var previousValue = totalMarketValue - dayGainLoss;
        portfolio.DayGainLossPercent = previousValue > 0 
            ? (dayGainLoss / previousValue) * 100 
            : 0;
        
        portfolio.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Create daily performance record
        await CreateDailyPerformanceRecordAsync(portfolioId);
    }

    public async Task<RebalanceRecommendationDto> GetRebalanceRecommendationAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await _context.Portfolios
            .Include(p => p.Holdings)
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId);

        if (portfolio == null)
        {
            throw new KeyNotFoundException("Portfolio not found");
        }

        var recommendation = new RebalanceRecommendationDto
        {
            PortfolioId = portfolioId,
            Actions = new List<RebalanceActionDto>(),
            EstimatedCost = 0,
            Reason = "Portfolio appears to be well balanced"
        };

        // Simple rebalancing logic - identify overweight positions
        var totalValue = portfolio.Holdings.Sum(h => h.MarketValue);
        if (totalValue > 0)
        {
            var overweightThreshold = 0.20m; // 20% threshold
            var overweightHoldings = portfolio.Holdings
                .Where(h => (h.MarketValue / totalValue) > overweightThreshold)
                .ToList();

            if (overweightHoldings.Any())
            {
                recommendation.Reason = "Some positions are overweight and may benefit from rebalancing";
                
                foreach (var holding in overweightHoldings)
                {
                    var currentWeight = h.MarketValue / totalValue;
                    var targetWeight = 0.15m; // Target 15%
                    var excessValue = (currentWeight - targetWeight) * totalValue;
                    
                    if (excessValue > 100) // Only recommend if excess is significant
                    {
                        var sellQuantity = excessValue / holding.CurrentPrice;
                        
                        recommendation.Actions.Add(new RebalanceActionDto
                        {
                            Symbol = holding.Symbol,
                            Action = "SELL",
                            Quantity = Math.Round(sellQuantity, 2),
                            EstimatedPrice = holding.CurrentPrice,
                            EstimatedValue = excessValue,
                            Reason = $"Reduce overweight position from {currentWeight:P1} to {targetWeight:P1}"
                        });

                        recommendation.EstimatedCost += 10; // Estimated commission
                    }
                }
            }
        }

        return recommendation;
    }

    private async Task<List<PortfolioAllocationDto>> GetPortfolioAllocationsAsync(Guid portfolioId)
    {
        var holdings = await _context.Holdings
            .Where(h => h.PortfolioId == portfolioId && h.Quantity > 0)
            .ToListAsync();

        var allocations = new Dictionary<string, decimal>();
        var totalValue = holdings.Sum(h => h.MarketValue);

        foreach (var holding in holdings)
        {
            // Get sector information from market data service
            var marketData = await _marketDataService.GetStockAsync(holding.Symbol);
            var sector = marketData?.Sector ?? "Unknown";

            if (!allocations.ContainsKey(sector))
            {
                allocations[sector] = 0;
            }
            allocations[sector] += holding.MarketValue;
        }

        return allocations.Select(kvp => new PortfolioAllocationDto
        {
            PortfolioId = portfolioId,
            Category = kvp.Key,
            Value = kvp.Value,
            Percentage = totalValue > 0 ? (kvp.Value / totalValue) * 100 : 0,
            CalculatedAt = DateTime.UtcNow
        }).ToList();
    }

    private async Task<List<PortfolioAllocationDto>> GetOverallAllocationAsync(Guid userId)
    {
        var allHoldings = await _context.Holdings
            .Include(h => h.Portfolio)
            .Where(h => h.Portfolio.UserId == userId && h.Quantity > 0)
            .ToListAsync();

        var allocations = new Dictionary<string, decimal>();
        var totalValue = allHoldings.Sum(h => h.MarketValue);

        foreach (var holding in allHoldings)
        {
            var marketData = await _marketDataService.GetStockAsync(holding.Symbol);
            var sector = marketData?.Sector ?? "Unknown";

            if (!allocations.ContainsKey(sector))
            {
                allocations[sector] = 0;
            }
            allocations[sector] += holding.MarketValue;
        }

        return allocations.Select(kvp => new PortfolioAllocationDto
        {
            Category = kvp.Key,
            Value = kvp.Value,
            Percentage = totalValue > 0 ? (kvp.Value / totalValue) * 100 : 0,
            CalculatedAt = DateTime.UtcNow
        }).ToList();
    }

    private async Task CreateDailyPerformanceRecordAsync(Guid portfolioId)
    {
        var today = DateTime.UtcNow.Date;
        
        // Check if record already exists for today
        var existingRecord = await _context.PortfolioPerformances
            .FirstOrDefaultAsync(pp => pp.PortfolioId == portfolioId && pp.Date == today);

        if (existingRecord != null) return;

        var portfolio = await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId);

        if (portfolio == null) return;

        var performanceRecord = new PortfolioPerformance
        {
            PortfolioId = portfolioId,
            Date = today,
            TotalValue = portfolio.CurrentValue,
            CashBalance = portfolio.CashBalance,
            InvestedAmount = portfolio.InvestedAmount,
            DayGainLoss = portfolio.DayGainLoss,
            DayGainLossPercent = portfolio.DayGainLossPercent,
            TotalGainLoss = portfolio.TotalGainLoss,
            TotalGainLossPercent = portfolio.TotalGainLossPercent
        };

        _context.PortfolioPerformances.Add(performanceRecord);
        await _context.SaveChangesAsync();
    }
}

// Market data service interface (implemented in MarketData.API)
public interface IMarketDataService
{
    Task<QuoteDto?> GetQuoteAsync(string symbol);
    Task<StockDto?> GetStockAsync(string symbol);
}

public class QuoteDto
{
    public string Symbol { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
}

public class StockDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Industry { get; set; }
}
