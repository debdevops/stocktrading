using Shared.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace TradingEngine.API.Models;

public class Order : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public OrderType OrderType { get; set; }
    
    [Required]
    public OrderSide Side { get; set; }
    
    [Required]
    public decimal Quantity { get; set; }
    
    public decimal? Price { get; set; } // Null for market orders
    
    public decimal? StopPrice { get; set; } // For stop orders
    
    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public decimal FilledQuantity { get; set; } = 0;
    
    public decimal? AveragePrice { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public TimeInForce TimeInForce { get; set; } = TimeInForce.Day;
    
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual ICollection<Trade> Trades { get; set; } = new List<Trade>();
}

public class Trade : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public decimal Quantity { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    [Required]
    public OrderSide Side { get; set; }
    
    public decimal Commission { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    public string? ExternalTradeId { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}

public class Position : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public decimal Quantity { get; set; }
    
    [Required]
    public decimal AveragePrice { get; set; }
    
    public decimal MarketValue { get; set; }
    
    public decimal UnrealizedPnL { get; set; }
    
    public decimal RealizedPnL { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class Watchlist : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<WatchlistItem> Items { get; set; } = new List<WatchlistItem>();
}

public class WatchlistItem : BaseEntity
{
    [Required]
    public Guid WatchlistId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    public int SortOrder { get; set; }
    
    public decimal? AlertPrice { get; set; }
    
    public AlertType? AlertType { get; set; }
    
    public bool AlertEnabled { get; set; } = false;
    
    // Navigation properties
    public virtual Watchlist Watchlist { get; set; } = null!;
}

public enum OrderType
{
    Market,
    Limit,
    Stop,
    StopLimit,
    TrailingStop
}

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderStatus
{
    Pending,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected,
    Expired
}

public enum TimeInForce
{
    Day,
    GTC, // Good Till Cancelled
    IOC, // Immediate Or Cancel
    FOK  // Fill Or Kill
}

public enum AlertType
{
    PriceAbove,
    PriceBelow,
    VolumeAbove,
    PercentChange
}
