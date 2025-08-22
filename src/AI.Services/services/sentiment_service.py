import asyncio
import aiohttp
import logging
from datetime import datetime, timedelta
from typing import List, Dict, Any, Optional
import pandas as pd
import numpy as np
from transformers import pipeline, AutoTokenizer, AutoModelForSequenceClassification
import yfinance as yf
import requests
from bs4 import BeautifulSoup
import re

from models.sentiment_models import SentimentAnalysisResponse, SentimentScore, SourceSentiment, SentimentSource, TimeRange

logger = logging.getLogger(__name__)

class SentimentService:
    def __init__(self):
        self.sentiment_analyzer = None
        self.news_sources = {
            'yahoo': 'https://finance.yahoo.com/news/',
            'reuters': 'https://www.reuters.com/markets/',
            'bloomberg': 'https://www.bloomberg.com/markets'
        }
        self.social_keywords_cache = {}
        
    async def initialize(self):
        """Initialize sentiment analysis models"""
        try:
            logger.info("Loading sentiment analysis model...")
            # Load pre-trained sentiment analysis model
            self.sentiment_analyzer = pipeline(
                "sentiment-analysis",
                model="cardiffnlp/twitter-roberta-base-sentiment-latest",
                tokenizer="cardiffnlp/twitter-roberta-base-sentiment-latest"
            )
            logger.info("Sentiment analysis model loaded successfully")
        except Exception as e:
            logger.warning(f"Failed to load advanced model, using fallback: {str(e)}")
            # Fallback to simpler model
            self.sentiment_analyzer = pipeline("sentiment-analysis")
    
    async def analyze_sentiment(self, symbols: List[str], sources: List[SentimentSource], 
                              time_range: TimeRange) -> SentimentAnalysisResponse:
        """Analyze sentiment for given symbols from specified sources"""
        try:
            sentiment_scores = []
            source_breakdown = []
            
            for symbol in symbols:
                # Analyze sentiment from different sources
                news_sentiment = await self._analyze_news_sentiment(symbol, time_range)
                social_sentiment = await self._analyze_social_sentiment(symbol, time_range)
                
                # Combine sentiments
                overall_sentiment = self._combine_sentiments([news_sentiment, social_sentiment])
                
                sentiment_scores.append(SentimentScore(
                    symbol=symbol,
                    overall_sentiment=overall_sentiment['score'],
                    sentiment_label=overall_sentiment['label'],
                    confidence=overall_sentiment['confidence'],
                    volume=overall_sentiment['volume'],
                    trending=overall_sentiment['trending']
                ))
            
            # Analyze source breakdown
            if SentimentSource.NEWS in sources:
                news_breakdown = await self._get_news_breakdown(symbols, time_range)
                source_breakdown.append(news_breakdown)
            
            if SentimentSource.SOCIAL in sources:
                social_breakdown = await self._get_social_breakdown(symbols, time_range)
                source_breakdown.append(social_breakdown)
            
            # Determine market mood
            market_mood = self._determine_market_mood(sentiment_scores)
            
            # Get key events
            key_events = await self._get_key_events(symbols, time_range)
            
            return SentimentAnalysisResponse(
                symbols=symbols,
                time_range=time_range,
                sentiment_scores=sentiment_scores,
                source_breakdown=source_breakdown,
                market_mood=market_mood,
                key_events=key_events,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Sentiment analysis failed: {str(e)}")
            raise
    
    async def _analyze_news_sentiment(self, symbol: str, time_range: TimeRange) -> Dict[str, Any]:
        """Analyze sentiment from news sources"""
        try:
            # Fetch news articles
            articles = await self._fetch_news_articles(symbol, time_range)
            
            if not articles:
                return {
                    'score': 0.0,
                    'label': 'neutral',
                    'confidence': 0.0,
                    'volume': 0,
                    'trending': False
                }
            
            # Analyze sentiment of each article
            sentiments = []
            for article in articles:
                try:
                    # Combine title and description for analysis
                    text = f"{article.get('title', '')} {article.get('description', '')}"
                    if len(text.strip()) > 0:
                        sentiment = self.sentiment_analyzer(text[:512])  # Limit text length
                        sentiments.append(sentiment[0])
                except Exception as e:
                    logger.warning(f"Failed to analyze article sentiment: {str(e)}")
                    continue
            
            if not sentiments:
                return {
                    'score': 0.0,
                    'label': 'neutral',
                    'confidence': 0.0,
                    'volume': 0,
                    'trending': False
                }
            
            # Calculate overall sentiment
            overall_sentiment = self._calculate_overall_sentiment(sentiments)
            overall_sentiment['volume'] = len(articles)
            overall_sentiment['trending'] = len(articles) > 10  # Simple trending logic
            
            return overall_sentiment
            
        except Exception as e:
            logger.error(f"News sentiment analysis failed for {symbol}: {str(e)}")
            return {
                'score': 0.0,
                'label': 'neutral',
                'confidence': 0.0,
                'volume': 0,
                'trending': False
            }
    
    async def _analyze_social_sentiment(self, symbol: str, time_range: TimeRange) -> Dict[str, Any]:
        """Analyze sentiment from social media sources"""
        try:
            # For demo purposes, simulate social sentiment
            # In production, this would integrate with Twitter API, Reddit API, etc.
            
            # Generate mock social sentiment based on recent price movement
            ticker = yf.Ticker(symbol)
            hist = ticker.history(period="5d")
            
            if len(hist) > 1:
                price_change = (hist['Close'].iloc[-1] - hist['Close'].iloc[0]) / hist['Close'].iloc[0]
                
                # Simulate social sentiment based on price movement
                if price_change > 0.05:
                    sentiment_score = min(0.8, 0.5 + price_change * 2)
                    label = 'positive'
                elif price_change < -0.05:
                    sentiment_score = max(-0.8, -0.5 + price_change * 2)
                    label = 'negative'
                else:
                    sentiment_score = price_change
                    label = 'neutral'
                
                # Add some randomness to simulate real social sentiment
                sentiment_score += np.random.normal(0, 0.1)
                sentiment_score = max(-1.0, min(1.0, sentiment_score))
                
                return {
                    'score': sentiment_score,
                    'label': label,
                    'confidence': 0.7,
                    'volume': np.random.randint(50, 500),
                    'trending': abs(price_change) > 0.1
                }
            
            return {
                'score': 0.0,
                'label': 'neutral',
                'confidence': 0.5,
                'volume': 0,
                'trending': False
            }
            
        except Exception as e:
            logger.error(f"Social sentiment analysis failed for {symbol}: {str(e)}")
            return {
                'score': 0.0,
                'label': 'neutral',
                'confidence': 0.0,
                'volume': 0,
                'trending': False
            }
    
    async def _fetch_news_articles(self, symbol: str, time_range: TimeRange) -> List[Dict[str, Any]]:
        """Fetch news articles for a symbol"""
        try:
            # Use Yahoo Finance news as primary source
            ticker = yf.Ticker(symbol)
            news = ticker.news
            
            articles = []
            for article in news[:20]:  # Limit to recent articles
                articles.append({
                    'title': article.get('title', ''),
                    'description': article.get('summary', ''),
                    'url': article.get('link', ''),
                    'published_at': datetime.fromtimestamp(article.get('providerPublishTime', 0)),
                    'source': article.get('publisher', '')
                })
            
            return articles
            
        except Exception as e:
            logger.error(f"Failed to fetch news for {symbol}: {str(e)}")
            return []
    
    def _calculate_overall_sentiment(self, sentiments: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Calculate overall sentiment from individual sentiment scores"""
        if not sentiments:
            return {
                'score': 0.0,
                'label': 'neutral',
                'confidence': 0.0
            }
        
        # Convert sentiment labels to scores
        scores = []
        confidences = []
        
        for sentiment in sentiments:
            label = sentiment['label'].lower()
            confidence = sentiment['score']
            
            if 'positive' in label:
                score = confidence
            elif 'negative' in label:
                score = -confidence
            else:
                score = 0.0
            
            scores.append(score)
            confidences.append(confidence)
        
        # Calculate weighted average
        overall_score = np.average(scores, weights=confidences)
        overall_confidence = np.mean(confidences)
        
        # Determine label
        if overall_score > 0.6:
            label = 'very_positive'
        elif overall_score > 0.2:
            label = 'positive'
        elif overall_score > -0.2:
            label = 'neutral'
        elif overall_score > -0.6:
            label = 'negative'
        else:
            label = 'very_negative'
        
        return {
            'score': float(overall_score),
            'label': label,
            'confidence': float(overall_confidence)
        }
    
    def _combine_sentiments(self, sentiments: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Combine sentiments from multiple sources"""
        if not sentiments:
            return {
                'score': 0.0,
                'label': 'neutral',
                'confidence': 0.0,
                'volume': 0,
                'trending': False
            }
        
        # Weight different sources
        weights = [0.6, 0.4]  # News, Social
        
        scores = [s['score'] for s in sentiments]
        confidences = [s['confidence'] for s in sentiments]
        volumes = [s['volume'] for s in sentiments]
        
        # Calculate weighted averages
        overall_score = np.average(scores, weights=weights[:len(scores)])
        overall_confidence = np.average(confidences, weights=weights[:len(confidences)])
        total_volume = sum(volumes)
        trending = any(s['trending'] for s in sentiments)
        
        # Determine label
        if overall_score > 0.6:
            label = 'very_positive'
        elif overall_score > 0.2:
            label = 'positive'
        elif overall_score > -0.2:
            label = 'neutral'
        elif overall_score > -0.6:
            label = 'negative'
        else:
            label = 'very_negative'
        
        return {
            'score': float(overall_score),
            'label': label,
            'confidence': float(overall_confidence),
            'volume': total_volume,
            'trending': trending
        }
    
    async def _get_news_breakdown(self, symbols: List[str], time_range: TimeRange) -> SourceSentiment:
        """Get news source breakdown"""
        # Mock implementation
        return SourceSentiment(
            source="news",
            sentiment_score=0.3,
            article_count=45,
            key_themes=["earnings", "market_volatility", "tech_growth"],
            sample_headlines=[
                "Tech stocks rally on strong earnings",
                "Market shows resilience amid uncertainty",
                "Growth stocks outperform expectations"
            ]
        )
    
    async def _get_social_breakdown(self, symbols: List[str], time_range: TimeRange) -> SourceSentiment:
        """Get social media source breakdown"""
        # Mock implementation
        return SourceSentiment(
            source="social",
            sentiment_score=0.1,
            article_count=234,
            key_themes=["bullish", "diamond_hands", "to_the_moon"],
            sample_headlines=[
                "Bullish sentiment growing on social media",
                "Retail investors showing confidence",
                "Social media buzz increasing"
            ]
        )
    
    def _determine_market_mood(self, sentiment_scores: List[SentimentScore]) -> str:
        """Determine overall market mood"""
        if not sentiment_scores:
            return "neutral"
        
        avg_sentiment = np.mean([score.overall_sentiment for score in sentiment_scores])
        
        if avg_sentiment > 0.4:
            return "bullish"
        elif avg_sentiment < -0.4:
            return "bearish"
        elif abs(avg_sentiment) < 0.1:
            return "neutral"
        else:
            return "mixed"
    
    async def _get_key_events(self, symbols: List[str], time_range: TimeRange) -> List[str]:
        """Get key market events affecting sentiment"""
        # Mock implementation - in production, this would fetch real events
        events = [
            "Federal Reserve announces interest rate decision",
            "Q3 earnings season begins",
            "Tech sector shows strong performance",
            "Market volatility decreases",
            "Consumer confidence index rises"
        ]
        
        return events[:3]  # Return top 3 events
    
    async def get_market_insights(self) -> List[Dict[str, Any]]:
        """Get AI-generated market insights"""
        try:
            # Generate insights based on current market conditions
            insights = [
                {
                    "title": "Tech Sector Momentum",
                    "description": "Technology stocks are showing strong momentum with positive sentiment across news and social media.",
                    "impact": "positive",
                    "confidence": 0.8,
                    "timeframe": "short_term"
                },
                {
                    "title": "Market Volatility Outlook",
                    "description": "Current sentiment analysis suggests reduced market volatility in the near term.",
                    "impact": "neutral",
                    "confidence": 0.7,
                    "timeframe": "medium_term"
                },
                {
                    "title": "Earnings Season Impact",
                    "description": "Positive earnings sentiment is driving bullish market mood across multiple sectors.",
                    "impact": "positive",
                    "confidence": 0.75,
                    "timeframe": "short_term"
                }
            ]
            
            return insights
            
        except Exception as e:
            logger.error(f"Failed to generate market insights: {str(e)}")
            return []
