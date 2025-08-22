from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from datetime import datetime
from enum import Enum

class SentimentSource(str, Enum):
    NEWS = "news"
    SOCIAL = "social"
    EARNINGS = "earnings"
    ANALYST = "analyst"

class TimeRange(str, Enum):
    ONE_DAY = "1d"
    THREE_DAYS = "3d"
    ONE_WEEK = "7d"
    TWO_WEEKS = "14d"
    ONE_MONTH = "30d"

class SentimentAnalysisRequest(BaseModel):
    symbols: List[str] = Field(..., description="List of stock symbols to analyze")
    sources: List[SentimentSource] = Field(default=[SentimentSource.NEWS, SentimentSource.SOCIAL], description="Sources to analyze sentiment from")
    time_range: TimeRange = Field(default=TimeRange.ONE_WEEK, description="Time range for sentiment analysis")
    include_keywords: bool = Field(default=True, description="Include keyword analysis")

class SentimentScore(BaseModel):
    symbol: str
    overall_sentiment: float  # -1 to 1 scale
    sentiment_label: str  # "very_negative", "negative", "neutral", "positive", "very_positive"
    confidence: float  # 0-1 scale
    volume: int  # Number of mentions/articles
    trending: bool  # Is this symbol trending

class SourceSentiment(BaseModel):
    source: str
    sentiment_score: float
    article_count: int
    key_themes: List[str]
    sample_headlines: List[str]

class SentimentAnalysisResponse(BaseModel):
    symbols: List[str]
    time_range: str
    sentiment_scores: List[SentimentScore]
    source_breakdown: List[SourceSentiment]
    market_mood: str  # "bullish", "bearish", "neutral", "mixed"
    key_events: List[str]
    generated_at: datetime
    metadata: Dict[str, Any] = {}
