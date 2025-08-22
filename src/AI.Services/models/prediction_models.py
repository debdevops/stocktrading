from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from datetime import datetime
from enum import Enum

class ModelType(str, Enum):
    LSTM = "lstm"
    PROPHET = "prophet"
    LINEAR_REGRESSION = "linear_regression"
    RANDOM_FOREST = "random_forest"
    ENSEMBLE = "ensemble"

class PredictionRequest(BaseModel):
    symbol: str = Field(..., description="Stock symbol to predict")
    days_ahead: int = Field(default=30, ge=1, le=365, description="Number of days to predict ahead")
    model_type: ModelType = Field(default=ModelType.ENSEMBLE, description="ML model to use for prediction")
    include_technical_indicators: bool = Field(default=True, description="Include technical indicators in prediction")

class PredictionPoint(BaseModel):
    date: datetime
    predicted_price: float
    confidence_interval_lower: float
    confidence_interval_upper: float
    confidence_score: float

class PredictionResponse(BaseModel):
    symbol: str
    current_price: float
    predictions: List[PredictionPoint]
    model_used: str
    accuracy_score: Optional[float] = None
    trend: str  # "bullish", "bearish", "neutral"
    risk_level: str  # "low", "medium", "high"
    recommendation: str  # "buy", "sell", "hold"
    generated_at: datetime
    metadata: Dict[str, Any] = {}

class TechnicalAnalysisRequest(BaseModel):
    symbol: str = Field(..., description="Stock symbol to analyze")
    period: str = Field(default="1y", description="Time period for analysis (1d, 5d, 1mo, 3mo, 6mo, 1y, 2y, 5y)")
    indicators: List[str] = Field(default=["RSI", "MACD", "BB", "SMA", "EMA"], description="Technical indicators to calculate")

class TechnicalIndicator(BaseModel):
    name: str
    value: float
    signal: str  # "buy", "sell", "neutral"
    description: str

class TechnicalAnalysisResponse(BaseModel):
    symbol: str
    period: str
    indicators: List[TechnicalIndicator]
    overall_signal: str  # "buy", "sell", "neutral"
    strength: float  # 0-1 scale
    support_levels: List[float]
    resistance_levels: List[float]
    trend_analysis: Dict[str, Any]
    generated_at: datetime
