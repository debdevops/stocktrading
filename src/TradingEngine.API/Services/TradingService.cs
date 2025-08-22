using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TradingEngine.API.Data;
using TradingEngine.API.DTOs;
using TradingEngine.API.Models;

namespace TradingEngine.API.Services;

public interface ITradingService
{
    Task<OrderExecutionResult> CreateOrderAsync(Guid userId, CreateOrderDto orderDto);
    Task<OrderDto> GetOrderAsync(Guid userId, Guid orderId);
    Task<List<OrderDto>> GetOrdersAsync(Guid userId, OrderFilterDto filter);
    Task<bool> CancelOrderAsync(Guid userId, Guid orderId, string? reason = null);
    Task<List<TradeDto>> GetTradesAsync(Guid userId, TradeFilterDto filter);
    Task<List<PositionDto>> GetPositionsAsync(Guid userId);
    Task<PositionDto?> GetPositionAsync(Guid userId, string symbol);
}

public class TradingService : ITradingService
{
    private readonly TradingDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TradingService> _logger;
    private readonly IMarketDataService _marketDataService;

    public TradingService(
        TradingDbContext context, 
        IMapper mapper, 
        ILogger<TradingService> logger,
        IMarketDataService marketDataService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _marketDataService = marketDataService;
    }

    public async Task<OrderExecutionResult> CreateOrderAsync(Guid userId, CreateOrderDto orderDto)
    {
        try
        {
            // Validate order
            await ValidateOrderAsync(userId, orderDto);

            // Create order entity
            var order = new Order
            {
                UserId = userId,
                Symbol = orderDto.Symbol.ToUpper(),
                OrderType = orderDto.OrderType,
                Side = orderDto.Side,
                Quantity = orderDto.Quantity,
                Price = orderDto.Price,
                StopPrice = orderDto.StopPrice,
                ExpiryDate = orderDto.ExpiryDate,
                TimeInForce = orderDto.TimeInForce,
                Notes = orderDto.Notes,
                Status = OrderStatus.Pending
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Execute order based on type
            var executionResult = await ExecuteOrderAsync(order);

            return executionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user {UserId}", userId);
            return new OrderExecutionResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<OrderDto> GetOrderAsync(Guid userId, Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Trades)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found");
        }

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<List<OrderDto>> GetOrdersAsync(Guid userId, OrderFilterDto filter)
    {
        var query = _context.Orders
            .Include(o => o.Trades)
            .Where(o => o.UserId == userId);

        if (!string.IsNullOrEmpty(filter.Symbol))
        {
            query = query.Where(o => o.Symbol == filter.Symbol.ToUpper());
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.Side.HasValue)
        {
            query = query.Where(o => o.Side == filter.Side.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<bool> CancelOrderAsync(Guid userId, Guid orderId, string? reason = null)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found");
        }

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.PartiallyFilled)
        {
            throw new InvalidOperationException("Order cannot be cancelled in its current state");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        order.Notes = string.IsNullOrEmpty(reason) ? order.Notes : $"{order.Notes} | Cancelled: {reason}";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} cancelled for user {UserId}", orderId, userId);
        return true;
    }

    public async Task<List<TradeDto>> GetTradesAsync(Guid userId, TradeFilterDto filter)
    {
        var query = _context.Trades.Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(filter.Symbol))
        {
            query = query.Where(t => t.Symbol == filter.Symbol.ToUpper());
        }

        if (filter.Side.HasValue)
        {
            query = query.Where(t => t.Side == filter.Side.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.ExecutedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(t => t.ExecutedAt <= filter.ToDate.Value);
        }

        var trades = await query
            .OrderByDescending(t => t.ExecutedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return _mapper.Map<List<TradeDto>>(trades);
    }

    public async Task<List<PositionDto>> GetPositionsAsync(Guid userId)
    {
        var positions = await _context.Positions
            .Where(p => p.UserId == userId && p.Quantity != 0)
            .ToListAsync();

        var positionDtos = new List<PositionDto>();

        foreach (var position in positions)
        {
            var positionDto = _mapper.Map<PositionDto>(position);
            
            // Get current market data
            var marketData = await _marketDataService.GetQuoteAsync(position.Symbol);
            if (marketData != null)
            {
                positionDto.CurrentPrice = marketData.Price;
                positionDto.MarketValue = position.Quantity * marketData.Price;
                positionDto.UnrealizedPnL = (marketData.Price - position.AveragePrice) * position.Quantity;
                positionDto.DayChange = marketData.Change;
                positionDto.DayChangePercent = marketData.ChangePercent;
            }

            positionDtos.Add(positionDto);
        }

        return positionDtos;
    }

    public async Task<PositionDto?> GetPositionAsync(Guid userId, string symbol)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == symbol.ToUpper());

        if (position == null)
        {
            return null;
        }

        var positionDto = _mapper.Map<PositionDto>(position);
        
        // Get current market data
        var marketData = await _marketDataService.GetQuoteAsync(position.Symbol);
        if (marketData != null)
        {
            positionDto.CurrentPrice = marketData.Price;
            positionDto.MarketValue = position.Quantity * marketData.Price;
            positionDto.UnrealizedPnL = (marketData.Price - position.AveragePrice) * position.Quantity;
            positionDto.DayChange = marketData.Change;
            positionDto.DayChangePercent = marketData.ChangePercent;
        }

        return positionDto;
    }

    private async Task ValidateOrderAsync(Guid userId, CreateOrderDto orderDto)
    {
        // Validate symbol exists
        var marketData = await _marketDataService.GetQuoteAsync(orderDto.Symbol);
        if (marketData == null)
        {
            throw new ArgumentException($"Invalid symbol: {orderDto.Symbol}");
        }

        // Validate price for limit orders
        if (orderDto.OrderType == OrderType.Limit && !orderDto.Price.HasValue)
        {
            throw new ArgumentException("Price is required for limit orders");
        }

        // Validate stop price for stop orders
        if ((orderDto.OrderType == OrderType.Stop || orderDto.OrderType == OrderType.StopLimit) && !orderDto.StopPrice.HasValue)
        {
            throw new ArgumentException("Stop price is required for stop orders");
        }

        // Additional validations can be added here (buying power, position limits, etc.)
    }

    private async Task<OrderExecutionResult> ExecuteOrderAsync(Order order)
    {
        try
        {
            // For demo purposes, we'll simulate order execution
            // In a real system, this would integrate with a broker API or matching engine

            if (order.OrderType == OrderType.Market)
            {
                return await ExecuteMarketOrderAsync(order);
            }
            else
            {
                // For limit orders, we'll mark as pending and simulate partial fills
                return new OrderExecutionResult
                {
                    Success = true,
                    Message = "Order placed successfully",
                    OrderId = order.Id
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing order {OrderId}", order.Id);
            
            order.Status = OrderStatus.Rejected;
            await _context.SaveChangesAsync();

            return new OrderExecutionResult
            {
                Success = false,
                Message = $"Order execution failed: {ex.Message}",
                OrderId = order.Id
            };
        }
    }

    private async Task<OrderExecutionResult> ExecuteMarketOrderAsync(Order order)
    {
        // Get current market price
        var marketData = await _marketDataService.GetQuoteAsync(order.Symbol);
        if (marketData == null)
        {
            throw new Exception("Unable to get market data for execution");
        }

        var executionPrice = order.Side == OrderSide.Buy ? marketData.Ask : marketData.Bid;
        var commission = CalculateCommission(order.Quantity, executionPrice);

        // Create trade
        var trade = new Trade
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Symbol = order.Symbol,
            Quantity = order.Quantity,
            Price = executionPrice,
            Side = order.Side,
            Commission = commission,
            TotalAmount = (order.Quantity * executionPrice) + commission,
            ExecutedAt = DateTime.UtcNow
        };

        _context.Trades.Add(trade);

        // Update order
        order.Status = OrderStatus.Filled;
        order.FilledQuantity = order.Quantity;
        order.AveragePrice = executionPrice;
        order.UpdatedAt = DateTime.UtcNow;

        // Update position
        await UpdatePositionAsync(order.UserId, order.Symbol, order.Side, order.Quantity, executionPrice);

        await _context.SaveChangesAsync();

        return new OrderExecutionResult
        {
            Success = true,
            Message = "Order executed successfully",
            OrderId = order.Id,
            Trades = new List<TradeDto> { _mapper.Map<TradeDto>(trade) },
            ExecutedQuantity = order.Quantity,
            AveragePrice = executionPrice
        };
    }

    private async Task UpdatePositionAsync(Guid userId, string symbol, OrderSide side, decimal quantity, decimal price)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == symbol);

        if (position == null)
        {
            // Create new position
            position = new Position
            {
                UserId = userId,
                Symbol = symbol,
                Quantity = side == OrderSide.Buy ? quantity : -quantity,
                AveragePrice = price,
                LastUpdated = DateTime.UtcNow
            };
            _context.Positions.Add(position);
        }
        else
        {
            // Update existing position
            var newQuantity = position.Quantity + (side == OrderSide.Buy ? quantity : -quantity);
            
            if (newQuantity == 0)
            {
                // Position closed
                position.RealizedPnL += (price - position.AveragePrice) * quantity * (side == OrderSide.Sell ? 1 : -1);
                position.Quantity = 0;
                position.AveragePrice = 0;
            }
            else if (Math.Sign(newQuantity) == Math.Sign(position.Quantity))
            {
                // Adding to position
                position.AveragePrice = ((position.Quantity * position.AveragePrice) + (quantity * price)) / (position.Quantity + quantity);
                position.Quantity = newQuantity;
            }
            else
            {
                // Reducing position
                var closedQuantity = Math.Min(Math.Abs(position.Quantity), quantity);
                position.RealizedPnL += (price - position.AveragePrice) * closedQuantity * (side == OrderSide.Sell ? 1 : -1);
                position.Quantity = newQuantity;
                
                if (newQuantity != 0 && Math.Sign(newQuantity) != Math.Sign(position.Quantity))
                {
                    position.AveragePrice = price;
                }
            }

            position.LastUpdated = DateTime.UtcNow;
        }
    }

    private decimal CalculateCommission(decimal quantity, decimal price)
    {
        // Simple commission calculation - $1 per trade + $0.005 per share
        return 1.0m + (quantity * 0.005m);
    }
}

// Market data service interface (will be implemented in MarketData.API)
public interface IMarketDataService
{
    Task<MarketQuote?> GetQuoteAsync(string symbol);
}

public class MarketQuote
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
