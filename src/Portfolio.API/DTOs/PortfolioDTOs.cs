using System.ComponentModel.DataAnnotations;
using Portfolio.API.Models;

namespace Portfolio.API.DTOs;

public class CreatePortfolioDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal InitialValue { get; set; }
    
    public bool IsDefault { get; set; } = false;
}

public class PortfolioDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
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
    public bool IsDefault { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<HoldingDto> Holdings { get; set; } = new();
    public List<PortfolioAllocationDto> Allocations { get; set; } = new();
}

public class HoldingDto
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnrealizedGainLoss { get; set; }
    public decimal UnrealizedGainLossPercent { get; set; }
    public decimal RealizedGainLoss { get; set; }
    public decimal DayGainLoss { get; set; }
    public decimal DayGainLossPercent { get; set; }
    public decimal AllocationPercent { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public Guid? OrderId { get; set; }
}

public class CreateTransactionDto
{
    [Required]
    public Guid PortfolioId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public TransactionType Type { get; set; }
    
    [Required]
    [Range(0.00000001, double.MaxValue)]
    public decimal Quantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal Commission { get; set; } = 0;
    
    public DateTime? TransactionDate { get; set; }
    
    public string? Notes { get; set; }
    
    public Guid? OrderId { get; set; }
}

public class PortfolioPerformanceDto
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal DayGainLoss { get; set; }
    public decimal DayGainLossPercent { get; set; }
    public decimal TotalGainLoss { get; set; }
    public decimal TotalGainLossPercent { get; set; }
}

public class PortfolioAllocationDto
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal Value { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class AlertDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public decimal TriggerValue { get; set; }
    public AlertCondition Condition { get; set; }
    public bool IsActive { get; set; }
    public bool IsTriggered { get; set; }
    public DateTime? TriggeredAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Message { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAlertDto
{
    public Guid? PortfolioId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public AlertType Type { get; set; }
    
    [Required]
    public decimal TriggerValue { get; set; }
    
    [Required]
    public AlertCondition Condition { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public string? Message { get; set; }
    
    public string? Notes { get; set; }
}

public class DividendRecordDto
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ExDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public DividendType Type { get; set; }
}

public class PortfolioSummaryDto
{
    public int TotalPortfolios { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalGainLoss { get; set; }
    public decimal TotalGainLossPercent { get; set; }
    public decimal DayGainLoss { get; set; }
    public decimal DayGainLossPercent { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public List<PortfolioDto> Portfolios { get; set; } = new();
    public List<HoldingDto> TopHoldings { get; set; } = new();
    public List<PortfolioAllocationDto> OverallAllocation { get; set; } = new();
}

public class PortfolioAnalyticsDto
{
    public Guid PortfolioId { get; set; }
    public string PortfolioName { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Beta { get; set; }
    public decimal Alpha { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public List<PortfolioPerformanceDto> PerformanceHistory { get; set; } = new();
}

public class TransactionFilterDto
{
    public Guid? PortfolioId { get; set; }
    public string? Symbol { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class PerformanceFilterDto
{
    public Guid PortfolioId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Period { get; set; } = "1M"; // 1D, 1W, 1M, 3M, 6M, 1Y, YTD, ALL
}

public class RebalanceRecommendationDto
{
    public Guid PortfolioId { get; set; }
    public List<RebalanceActionDto> Actions { get; set; } = new();
    public decimal EstimatedCost { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RebalanceActionDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // BUY, SELL
    public decimal Quantity { get; set; }
    public decimal EstimatedPrice { get; set; }
    public decimal EstimatedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
}
