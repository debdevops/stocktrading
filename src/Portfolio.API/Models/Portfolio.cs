using Shared.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Portfolio.API.Models;

public class Portfolio : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public decimal InitialValue { get; set; }
    
    public decimal CurrentValue { get; set; }
    
    public decimal TotalGainLoss { get; set; }
    
    public decimal TotalGainLossPercent { get; set; }
    
    public decimal DayGainLoss { get; set; }
    
    public decimal DayGainLossPercent { get; set; }
    
    public decimal CashBalance { get; set; }
    
    public decimal InvestedAmount { get; set; }
    
    public bool IsDefault { get; set; } = false;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<PortfolioPerformance> PerformanceHistory { get; set; } = new List<PortfolioPerformance>();
}

public class Holding : BaseEntity
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public decimal Quantity { get; set; }
    
    [Required]
    public decimal AverageCost { get; set; }
    
    public decimal CurrentPrice { get; set; }
    
    public decimal MarketValue { get; set; }
    
    public decimal TotalCost { get; set; }
    
    public decimal UnrealizedGainLoss { get; set; }
    
    public decimal UnrealizedGainLossPercent { get; set; }
    
    public decimal RealizedGainLoss { get; set; }
    
    public decimal DayGainLoss { get; set; }
    
    public decimal DayGainLossPercent { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
}

public class Transaction : BaseEntity
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public TransactionType Type { get; set; }
    
    [Required]
    public decimal Quantity { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    public decimal Commission { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
    
    public Guid? OrderId { get; set; } // Reference to trading order
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
}

public class PortfolioPerformance : BaseEntity
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    public decimal TotalValue { get; set; }
    
    public decimal CashBalance { get; set; }
    
    public decimal InvestedAmount { get; set; }
    
    public decimal DayGainLoss { get; set; }
    
    public decimal DayGainLossPercent { get; set; }
    
    public decimal TotalGainLoss { get; set; }
    
    public decimal TotalGainLossPercent { get; set; }
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
}

public class Alert : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    public Guid? PortfolioId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public AlertType Type { get; set; }
    
    [Required]
    public decimal TriggerValue { get; set; }
    
    public AlertCondition Condition { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsTriggered { get; set; } = false;
    
    public DateTime? TriggeredAt { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public string? Message { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Portfolio? Portfolio { get; set; }
}

public class PortfolioAllocation : BaseEntity
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // Sector, Asset Class, etc.
    
    [Required]
    public decimal Percentage { get; set; }
    
    public decimal Value { get; set; }
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
}

public class DividendRecord : BaseEntity
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public decimal Amount { get; set; }
    
    public decimal Quantity { get; set; }
    
    public DateTime ExDate { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public DividendType Type { get; set; } = DividendType.Cash;
    
    // Navigation properties
    public virtual Portfolio Portfolio { get; set; } = null!;
}

public enum TransactionType
{
    Buy,
    Sell,
    Dividend,
    Split,
    Deposit,
    Withdrawal,
    Fee,
    Interest
}

public enum AlertType
{
    Price,
    PriceChange,
    Volume,
    MarketCap,
    PortfolioValue,
    GainLoss
}

public enum AlertCondition
{
    Above,
    Below,
    Equals,
    PercentChange
}

public enum DividendType
{
    Cash,
    Stock,
    Special
}
