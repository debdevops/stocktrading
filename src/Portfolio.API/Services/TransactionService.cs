using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Portfolio.API.Data;
using Portfolio.API.DTOs;
using Portfolio.API.Models;

namespace Portfolio.API.Services;

public interface ITransactionService
{
    Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto transactionDto);
    Task<List<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterDto filter);
    Task<TransactionDto> GetTransactionAsync(Guid userId, Guid transactionId);
    Task<bool> DeleteTransactionAsync(Guid userId, Guid transactionId);
    Task ProcessTradeAsync(Guid userId, Guid portfolioId, string symbol, TransactionType type, decimal quantity, decimal price, decimal commission, Guid? orderId = null);
}

public class TransactionService : ITransactionService
{
    private readonly PortfolioDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TransactionService> _logger;
    private readonly IMarketDataService _marketDataService;

    public TransactionService(
        PortfolioDbContext context,
        IMapper mapper,
        ILogger<TransactionService> logger,
        IMarketDataService marketDataService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _marketDataService = marketDataService;
    }

    public async Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto transactionDto)
    {
        // Verify portfolio ownership
        var portfolio = await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Id == transactionDto.PortfolioId && p.UserId == userId);

        if (portfolio == null)
        {
            throw new KeyNotFoundException("Portfolio not found");
        }

        var transaction = new Transaction
        {
            PortfolioId = transactionDto.PortfolioId,
            Symbol = transactionDto.Symbol.ToUpper(),
            Type = transactionDto.Type,
            Quantity = transactionDto.Quantity,
            Price = transactionDto.Price,
            Commission = transactionDto.Commission,
            TotalAmount = CalculateTotalAmount(transactionDto.Type, transactionDto.Quantity, transactionDto.Price, transactionDto.Commission),
            TransactionDate = transactionDto.TransactionDate ?? DateTime.UtcNow,
            Notes = transactionDto.Notes,
            OrderId = transactionDto.OrderId
        };

        _context.Transactions.Add(transaction);

        // Update holdings and portfolio
        await UpdateHoldingsAsync(transactionDto.PortfolioId, transaction);
        await UpdatePortfolioCashAsync(transactionDto.PortfolioId, transaction);

        await _context.SaveChangesAsync();

        return await GetTransactionAsync(userId, transaction.Id);
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilterDto filter)
    {
        var query = _context.Transactions
            .Include(t => t.Portfolio)
            .Where(t => t.Portfolio.UserId == userId);

        if (filter.PortfolioId.HasValue)
        {
            query = query.Where(t => t.PortfolioId == filter.PortfolioId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Symbol))
        {
            query = query.Where(t => t.Symbol == filter.Symbol.ToUpper());
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var transactionDtos = new List<TransactionDto>();

        foreach (var transaction in transactions)
        {
            var transactionDto = _mapper.Map<TransactionDto>(transaction);
            
            // Get company name from market data
            var marketData = await _marketDataService.GetStockAsync(transaction.Symbol);
            transactionDto.CompanyName = marketData?.CompanyName ?? transaction.Symbol;

            transactionDtos.Add(transactionDto);
        }

        return transactionDtos;
    }

    public async Task<TransactionDto> GetTransactionAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Portfolio)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Portfolio.UserId == userId);

        if (transaction == null)
        {
            throw new KeyNotFoundException("Transaction not found");
        }

        var transactionDto = _mapper.Map<TransactionDto>(transaction);
        
        // Get company name from market data
        var marketData = await _marketDataService.GetStockAsync(transaction.Symbol);
        transactionDto.CompanyName = marketData?.CompanyName ?? transaction.Symbol;

        return transactionDto;
    }

    public async Task<bool> DeleteTransactionAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Portfolio)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Portfolio.UserId == userId);

        if (transaction == null)
        {
            throw new KeyNotFoundException("Transaction not found");
        }

        // Reverse the transaction effects
        await ReverseTransactionAsync(transaction);

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task ProcessTradeAsync(Guid userId, Guid portfolioId, string symbol, TransactionType type, decimal quantity, decimal price, decimal commission, Guid? orderId = null)
    {
        var createTransactionDto = new CreateTransactionDto
        {
            PortfolioId = portfolioId,
            Symbol = symbol,
            Type = type,
            Quantity = quantity,
            Price = price,
            Commission = commission,
            OrderId = orderId
        };

        await CreateTransactionAsync(userId, createTransactionDto);
    }

    private async Task UpdateHoldingsAsync(Guid portfolioId, Transaction transaction)
    {
        if (transaction.Type != TransactionType.Buy && transaction.Type != TransactionType.Sell)
        {
            return; // Only process buy/sell transactions for holdings
        }

        var holding = await _context.Holdings
            .FirstOrDefaultAsync(h => h.PortfolioId == portfolioId && h.Symbol == transaction.Symbol);

        if (holding == null && transaction.Type == TransactionType.Buy)
        {
            // Create new holding
            holding = new Holding
            {
                PortfolioId = portfolioId,
                Symbol = transaction.Symbol,
                Quantity = transaction.Quantity,
                AverageCost = transaction.Price,
                TotalCost = transaction.Quantity * transaction.Price,
                LastUpdated = DateTime.UtcNow
            };
            _context.Holdings.Add(holding);
        }
        else if (holding != null)
        {
            if (transaction.Type == TransactionType.Buy)
            {
                // Add to existing holding
                var newTotalCost = holding.TotalCost + (transaction.Quantity * transaction.Price);
                var newQuantity = holding.Quantity + transaction.Quantity;
                
                holding.AverageCost = newTotalCost / newQuantity;
                holding.Quantity = newQuantity;
                holding.TotalCost = newTotalCost;
            }
            else if (transaction.Type == TransactionType.Sell)
            {
                // Reduce holding
                if (transaction.Quantity > holding.Quantity)
                {
                    throw new InvalidOperationException("Cannot sell more shares than owned");
                }

                var soldCost = holding.AverageCost * transaction.Quantity;
                holding.RealizedGainLoss += (transaction.Price * transaction.Quantity) - soldCost;
                holding.Quantity -= transaction.Quantity;
                holding.TotalCost -= soldCost;

                if (holding.Quantity == 0)
                {
                    holding.AverageCost = 0;
                    holding.TotalCost = 0;
                }
            }

            holding.LastUpdated = DateTime.UtcNow;
        }
        else if (transaction.Type == TransactionType.Sell)
        {
            throw new InvalidOperationException("Cannot sell shares that are not owned");
        }
    }

    private async Task UpdatePortfolioCashAsync(Guid portfolioId, Transaction transaction)
    {
        var portfolio = await _context.Portfolios.FindAsync(portfolioId);
        if (portfolio == null) return;

        switch (transaction.Type)
        {
            case TransactionType.Buy:
                portfolio.CashBalance -= transaction.TotalAmount;
                break;
            case TransactionType.Sell:
                portfolio.CashBalance += transaction.TotalAmount - transaction.Commission;
                break;
            case TransactionType.Deposit:
                portfolio.CashBalance += transaction.TotalAmount;
                break;
            case TransactionType.Withdrawal:
                portfolio.CashBalance -= transaction.TotalAmount;
                break;
            case TransactionType.Dividend:
                portfolio.CashBalance += transaction.TotalAmount;
                break;
            case TransactionType.Fee:
                portfolio.CashBalance -= transaction.TotalAmount;
                break;
            case TransactionType.Interest:
                portfolio.CashBalance += transaction.TotalAmount;
                break;
        }

        portfolio.LastUpdated = DateTime.UtcNow;
    }

    private async Task ReverseTransactionAsync(Transaction transaction)
    {
        // Reverse the holding changes
        var holding = await _context.Holdings
            .FirstOrDefaultAsync(h => h.PortfolioId == transaction.PortfolioId && h.Symbol == transaction.Symbol);

        if (holding != null && (transaction.Type == TransactionType.Buy || transaction.Type == TransactionType.Sell))
        {
            if (transaction.Type == TransactionType.Buy)
            {
                // Reverse buy transaction
                var transactionCost = transaction.Quantity * transaction.Price;
                var newQuantity = holding.Quantity - transaction.Quantity;
                
                if (newQuantity < 0)
                {
                    throw new InvalidOperationException("Cannot reverse transaction: would result in negative holdings");
                }

                if (newQuantity == 0)
                {
                    holding.Quantity = 0;
                    holding.AverageCost = 0;
                    holding.TotalCost = 0;
                }
                else
                {
                    holding.TotalCost -= transactionCost;
                    holding.Quantity = newQuantity;
                    holding.AverageCost = holding.TotalCost / newQuantity;
                }
            }
            else if (transaction.Type == TransactionType.Sell)
            {
                // Reverse sell transaction
                var soldCost = holding.AverageCost * transaction.Quantity;
                holding.Quantity += transaction.Quantity;
                holding.TotalCost += soldCost;
                holding.RealizedGainLoss -= (transaction.Price * transaction.Quantity) - soldCost;
                
                if (holding.Quantity > 0)
                {
                    holding.AverageCost = holding.TotalCost / holding.Quantity;
                }
            }

            holding.LastUpdated = DateTime.UtcNow;
        }

        // Reverse cash changes
        var portfolio = await _context.Portfolios.FindAsync(transaction.PortfolioId);
        if (portfolio != null)
        {
            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    portfolio.CashBalance += transaction.TotalAmount;
                    break;
                case TransactionType.Sell:
                    portfolio.CashBalance -= (transaction.TotalAmount - transaction.Commission);
                    break;
                case TransactionType.Deposit:
                    portfolio.CashBalance -= transaction.TotalAmount;
                    break;
                case TransactionType.Withdrawal:
                    portfolio.CashBalance += transaction.TotalAmount;
                    break;
                case TransactionType.Dividend:
                    portfolio.CashBalance -= transaction.TotalAmount;
                    break;
                case TransactionType.Fee:
                    portfolio.CashBalance += transaction.TotalAmount;
                    break;
                case TransactionType.Interest:
                    portfolio.CashBalance -= transaction.TotalAmount;
                    break;
            }

            portfolio.LastUpdated = DateTime.UtcNow;
        }
    }

    private decimal CalculateTotalAmount(TransactionType type, decimal quantity, decimal price, decimal commission)
    {
        var baseAmount = quantity * price;
        
        return type switch
        {
            TransactionType.Buy => baseAmount + commission,
            TransactionType.Sell => baseAmount - commission,
            TransactionType.Deposit => baseAmount,
            TransactionType.Withdrawal => baseAmount,
            TransactionType.Dividend => baseAmount,
            TransactionType.Fee => commission,
            TransactionType.Interest => baseAmount,
            _ => baseAmount
        };
    }
}
