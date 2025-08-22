from fastapi import FastAPI, HTTPException, Depends, Security
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
import uvicorn
import logging
from typing import List, Optional
from datetime import datetime, timedelta
import os
from dotenv import load_dotenv

from models.prediction_models import PredictionRequest, PredictionResponse, TechnicalAnalysisRequest, TechnicalAnalysisResponse
from models.sentiment_models import SentimentAnalysisRequest, SentimentAnalysisResponse
from models.portfolio_models import PortfolioOptimizationRequest, PortfolioOptimizationResponse
from services.prediction_service import PredictionService
from services.sentiment_service import SentimentService
from services.portfolio_optimizer import PortfolioOptimizer
from services.technical_analyzer import TechnicalAnalyzer
from utils.auth import verify_jwt_token
from utils.logger import setup_logger

# Load environment variables
load_dotenv()

# Setup logging
logger = setup_logger(__name__)

# Initialize services
prediction_service = PredictionService()
sentiment_service = SentimentService()
portfolio_optimizer = PortfolioOptimizer()
technical_analyzer = TechnicalAnalyzer()

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    logger.info("Starting AI Services...")
    await prediction_service.initialize()
    await sentiment_service.initialize()
    logger.info("AI Services started successfully")
    yield
    # Shutdown
    logger.info("Shutting down AI Services...")

app = FastAPI(
    title="Stock Trading AI Services",
    description="AI-powered services for stock trading including predictions, sentiment analysis, and portfolio optimization",
    version="1.0.0",
    lifespan=lifespan
)

# Security
security = HTTPBearer()

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

async def get_current_user(credentials: HTTPAuthorizationCredentials = Security(security)):
    """Verify JWT token and extract user information"""
    try:
        token = credentials.credentials
        payload = verify_jwt_token(token)
        return payload
    except Exception as e:
        logger.error(f"Authentication failed: {str(e)}")
        raise HTTPException(status_code=401, detail="Invalid authentication credentials")

@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat(),
        "services": {
            "prediction": "active",
            "sentiment": "active",
            "portfolio_optimization": "active",
            "technical_analysis": "active"
        }
    }

@app.post("/api/ai/predictions/price", response_model=PredictionResponse)
async def predict_stock_price(
    request: PredictionRequest,
    current_user: dict = Depends(get_current_user)
):
    """Predict stock price using machine learning models"""
    try:
        logger.info(f"Price prediction request for {request.symbol} by user {current_user.get('user_id')}")
        prediction = await prediction_service.predict_price(
            symbol=request.symbol,
            days_ahead=request.days_ahead,
            model_type=request.model_type
        )
        return prediction
    except Exception as e:
        logger.error(f"Price prediction failed: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Prediction failed: {str(e)}")

@app.post("/api/ai/analysis/technical", response_model=TechnicalAnalysisResponse)
async def analyze_technical_indicators(
    request: TechnicalAnalysisRequest,
    current_user: dict = Depends(get_current_user)
):
    """Perform technical analysis on stock data"""
    try:
        logger.info(f"Technical analysis request for {request.symbol} by user {current_user.get('user_id')}")
        analysis = await technical_analyzer.analyze(
            symbol=request.symbol,
            period=request.period,
            indicators=request.indicators
        )
        return analysis
    except Exception as e:
        logger.error(f"Technical analysis failed: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Technical analysis failed: {str(e)}")

@app.post("/api/ai/sentiment/analyze", response_model=SentimentAnalysisResponse)
async def analyze_sentiment(
    request: SentimentAnalysisRequest,
    current_user: dict = Depends(get_current_user)
):
    """Analyze market sentiment for stocks"""
    try:
        logger.info(f"Sentiment analysis request for {request.symbols} by user {current_user.get('user_id')}")
        sentiment = await sentiment_service.analyze_sentiment(
            symbols=request.symbols,
            sources=request.sources,
            time_range=request.time_range
        )
        return sentiment
    except Exception as e:
        logger.error(f"Sentiment analysis failed: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Sentiment analysis failed: {str(e)}")

@app.post("/api/ai/portfolio/optimize", response_model=PortfolioOptimizationResponse)
async def optimize_portfolio(
    request: PortfolioOptimizationRequest,
    current_user: dict = Depends(get_current_user)
):
    """Optimize portfolio allocation using modern portfolio theory"""
    try:
        logger.info(f"Portfolio optimization request by user {current_user.get('user_id')}")
        optimization = await portfolio_optimizer.optimize(
            symbols=request.symbols,
            risk_tolerance=request.risk_tolerance,
            investment_amount=request.investment_amount,
            constraints=request.constraints
        )
        return optimization
    except Exception as e:
        logger.error(f"Portfolio optimization failed: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Portfolio optimization failed: {str(e)}")

@app.get("/api/ai/predictions/trending")
async def get_trending_predictions(
    limit: int = 10,
    current_user: dict = Depends(get_current_user)
):
    """Get trending stock predictions"""
    try:
        trending = await prediction_service.get_trending_predictions(limit)
        return {"predictions": trending, "timestamp": datetime.utcnow().isoformat()}
    except Exception as e:
        logger.error(f"Failed to get trending predictions: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to get trending predictions: {str(e)}")

@app.get("/api/ai/market/insights")
async def get_market_insights(
    current_user: dict = Depends(get_current_user)
):
    """Get AI-generated market insights"""
    try:
        insights = await sentiment_service.get_market_insights()
        return {"insights": insights, "timestamp": datetime.utcnow().isoformat()}
    except Exception as e:
        logger.error(f"Failed to get market insights: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to get market insights: {str(e)}")

@app.get("/api/ai/recommendations/{symbol}")
async def get_stock_recommendations(
    symbol: str,
    current_user: dict = Depends(get_current_user)
):
    """Get AI-powered stock recommendations"""
    try:
        # Combine technical analysis, sentiment, and price prediction
        technical_analysis = await technical_analyzer.analyze(symbol, "1y", ["RSI", "MACD", "BB"])
        sentiment = await sentiment_service.analyze_sentiment([symbol], ["news", "social"], "7d")
        prediction = await prediction_service.predict_price(symbol, 30, "ensemble")
        
        # Generate recommendation based on combined analysis
        recommendation = await prediction_service.generate_recommendation(
            symbol, technical_analysis, sentiment, prediction
        )
        
        return {
            "symbol": symbol,
            "recommendation": recommendation,
            "technical_analysis": technical_analysis,
            "sentiment": sentiment,
            "price_prediction": prediction,
            "timestamp": datetime.utcnow().isoformat()
        }
    except Exception as e:
        logger.error(f"Failed to get recommendations for {symbol}: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to get recommendations: {str(e)}")

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info"
    )
