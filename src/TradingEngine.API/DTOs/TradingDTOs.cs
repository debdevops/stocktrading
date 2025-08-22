using System.ComponentModel.DataAnnotations;
using TradingEngine.API.Models;

namespace TradingEngine.API.DTOs;

public class CreateOrderDto
{
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public OrderType OrderType { get; set; }
    
    [Required]
    public OrderSide Side { get; set; }
    
    [Required]
    [Range(0.00000001, double.MaxValue)]
    public decimal Quantity { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal? StopPrice { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public TimeInForce TimeInForce { get; set; } = TimeInForce.Day;
    
    public string? Notes { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? StopPrice { get; set; }
    public OrderStatus Status { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal? AveragePrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public TimeInForce TimeInForce { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TradeDto> Trades { get; set; } = new();
}

public class TradeDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public OrderSide Side { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? ExternalTradeId { get; set; }
}

public class PositionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CreateWatchlistDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; } = false;
}

public class WatchlistDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WatchlistItemDto> Items { get; set; } = new();
}

public class AddWatchlistItemDto
{
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    public decimal? AlertPrice { get; set; }
    
    public AlertType? AlertType { get; set; }
    
    public bool AlertEnabled { get; set; } = false;
}

public class WatchlistItemDto
{
    public Guid Id { get; set; }
    public Guid WatchlistId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal? AlertPrice { get; set; }
    public AlertType? AlertType { get; set; }
    public bool AlertEnabled { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public decimal Volume { get; set; }
}

public class OrderFilterDto
{
    public string? Symbol { get; set; }
    public OrderStatus? Status { get; set; }
    public OrderSide? Side { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class TradeFilterDto
{
    public string? Symbol { get; set; }
    public OrderSide? Side { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CancelOrderDto
{
    [Required]
    public Guid OrderId { get; set; }
    
    public string? Reason { get; set; }
}

public class OrderExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public List<TradeDto> Trades { get; set; } = new();
    public decimal? ExecutedQuantity { get; set; }
    public decimal? AveragePrice { get; set; }
}
