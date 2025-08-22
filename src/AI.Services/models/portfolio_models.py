from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from datetime import datetime
from enum import Enum

class RiskTolerance(str, Enum):
    CONSERVATIVE = "conservative"
    MODERATE = "moderate"
    AGGRESSIVE = "aggressive"

class OptimizationObjective(str, Enum):
    MAX_RETURN = "max_return"
    MIN_RISK = "min_risk"
    MAX_SHARPE = "max_sharpe"
    EQUAL_WEIGHT = "equal_weight"

class PortfolioConstraint(BaseModel):
    type: str  # "max_weight", "min_weight", "sector_limit"
    symbol: Optional[str] = None
    sector: Optional[str] = None
    value: float

class PortfolioOptimizationRequest(BaseModel):
    symbols: List[str] = Field(..., description="List of stock symbols to include in portfolio")
    risk_tolerance: RiskTolerance = Field(default=RiskTolerance.MODERATE, description="Risk tolerance level")
    investment_amount: float = Field(..., gt=0, description="Total amount to invest")
    objective: OptimizationObjective = Field(default=OptimizationObjective.MAX_SHARPE, description="Optimization objective")
    constraints: List[PortfolioConstraint] = Field(default=[], description="Portfolio constraints")
    rebalance_frequency: str = Field(default="monthly", description="How often to rebalance")

class AllocationRecommendation(BaseModel):
    symbol: str
    company_name: str
    recommended_weight: float
    recommended_shares: int
    recommended_amount: float
    expected_return: float
    risk_contribution: float
    sector: str

class RiskMetrics(BaseModel):
    portfolio_volatility: float
    value_at_risk_95: float
    expected_return: float
    sharpe_ratio: float
    max_drawdown: float
    beta: float

class PortfolioOptimizationResponse(BaseModel):
    allocations: List[AllocationRecommendation]
    risk_metrics: RiskMetrics
    total_investment: float
    expected_annual_return: float
    optimization_score: float
    rebalancing_suggestions: List[str]
    diversification_score: float
    generated_at: datetime
    metadata: Dict[str, Any] = {}
