import numpy as np
import pandas as pd
import yfinance as yf
from datetime import datetime, timedelta
from typing import List, Dict, Any, Optional
import logging
from sklearn.ensemble import RandomForestRegressor
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_squared_error, r2_score
import tensorflow as tf
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, Dropout
from prophet import Prophet
import ta
import joblib
import os

from models.prediction_models import PredictionResponse, PredictionPoint, ModelType

logger = logging.getLogger(__name__)

class PredictionService:
    def __init__(self):
        self.models = {}
        self.scalers = {}
        self.model_cache = {}
        
    async def initialize(self):
        """Initialize the prediction service"""
        logger.info("Initializing Prediction Service...")
        # Create models directory if it doesn't exist
        os.makedirs("models/saved", exist_ok=True)
        logger.info("Prediction Service initialized successfully")
    
    async def predict_price(self, symbol: str, days_ahead: int, model_type: str) -> PredictionResponse:
        """Predict stock price using specified model"""
        try:
            # Fetch historical data
            data = await self._fetch_stock_data(symbol)
            
            if model_type == ModelType.ENSEMBLE:
                prediction = await self._ensemble_predict(data, symbol, days_ahead)
            elif model_type == ModelType.LSTM:
                prediction = await self._lstm_predict(data, symbol, days_ahead)
            elif model_type == ModelType.PROPHET:
                prediction = await self._prophet_predict(data, symbol, days_ahead)
            elif model_type == ModelType.RANDOM_FOREST:
                prediction = await self._random_forest_predict(data, symbol, days_ahead)
            else:
                prediction = await self._linear_regression_predict(data, symbol, days_ahead)
            
            return prediction
            
        except Exception as e:
            logger.error(f"Prediction failed for {symbol}: {str(e)}")
            raise
    
    async def _fetch_stock_data(self, symbol: str, period: str = "2y") -> pd.DataFrame:
        """Fetch stock data from Yahoo Finance"""
        try:
            ticker = yf.Ticker(symbol)
            data = ticker.history(period=period)
            
            if data.empty:
                raise ValueError(f"No data found for symbol {symbol}")
            
            # Add technical indicators
            data = self._add_technical_indicators(data)
            
            return data
            
        except Exception as e:
            logger.error(f"Failed to fetch data for {symbol}: {str(e)}")
            raise
    
    def _add_technical_indicators(self, data: pd.DataFrame) -> pd.DataFrame:
        """Add technical indicators to the data"""
        # Moving averages
        data['SMA_20'] = ta.trend.sma_indicator(data['Close'], window=20)
        data['SMA_50'] = ta.trend.sma_indicator(data['Close'], window=50)
        data['EMA_12'] = ta.trend.ema_indicator(data['Close'], window=12)
        data['EMA_26'] = ta.trend.ema_indicator(data['Close'], window=26)
        
        # RSI
        data['RSI'] = ta.momentum.rsi(data['Close'])
        
        # MACD
        data['MACD'] = ta.trend.macd_diff(data['Close'])
        
        # Bollinger Bands
        bb = ta.volatility.BollingerBands(data['Close'])
        data['BB_upper'] = bb.bollinger_hband()
        data['BB_lower'] = bb.bollinger_lband()
        data['BB_middle'] = bb.bollinger_mavg()
        
        # Volume indicators
        data['Volume_SMA'] = ta.volume.volume_sma(data['Close'], data['Volume'])
        
        # Price changes
        data['Price_Change'] = data['Close'].pct_change()
        data['Price_Change_5d'] = data['Close'].pct_change(5)
        
        return data
    
    async def _lstm_predict(self, data: pd.DataFrame, symbol: str, days_ahead: int) -> PredictionResponse:
        """LSTM-based price prediction"""
        try:
            # Prepare data for LSTM
            features = ['Close', 'Volume', 'SMA_20', 'SMA_50', 'RSI', 'MACD']
            df = data[features].dropna()
            
            # Scale the data
            scaler = StandardScaler()
            scaled_data = scaler.fit_transform(df)
            
            # Create sequences
            sequence_length = 60
            X, y = self._create_sequences(scaled_data[:, 0], sequence_length)
            
            # Split data
            train_size = int(len(X) * 0.8)
            X_train, X_test = X[:train_size], X[train_size:]
            y_train, y_test = y[:train_size], y[train_size:]
            
            # Build LSTM model
            model = Sequential([
                LSTM(50, return_sequences=True, input_shape=(sequence_length, 1)),
                Dropout(0.2),
                LSTM(50, return_sequences=False),
                Dropout(0.2),
                Dense(25),
                Dense(1)
            ])
            
            model.compile(optimizer='adam', loss='mean_squared_error')
            
            # Train model
            model.fit(X_train, y_train, batch_size=32, epochs=50, verbose=0)
            
            # Make predictions
            predictions = []
            current_sequence = scaled_data[-sequence_length:, 0].reshape(1, sequence_length, 1)
            
            for _ in range(days_ahead):
                pred = model.predict(current_sequence, verbose=0)[0, 0]
                predictions.append(pred)
                
                # Update sequence
                current_sequence = np.roll(current_sequence, -1)
                current_sequence[0, -1, 0] = pred
            
            # Inverse transform predictions
            predictions = scaler.inverse_transform(
                np.column_stack([predictions, np.zeros((len(predictions), scaled_data.shape[1] - 1))])
            )[:, 0]
            
            # Create prediction points
            prediction_points = []
            start_date = data.index[-1] + timedelta(days=1)
            
            for i, pred_price in enumerate(predictions):
                confidence = max(0.6, 1.0 - (i * 0.01))  # Decreasing confidence over time
                
                prediction_points.append(PredictionPoint(
                    date=start_date + timedelta(days=i),
                    predicted_price=float(pred_price),
                    confidence_interval_lower=float(pred_price * 0.95),
                    confidence_interval_upper=float(pred_price * 1.05),
                    confidence_score=confidence
                ))
            
            # Calculate trend and recommendation
            trend = "bullish" if predictions[-1] > data['Close'].iloc[-1] else "bearish"
            recommendation = self._generate_recommendation_signal(data, predictions)
            
            return PredictionResponse(
                symbol=symbol,
                current_price=float(data['Close'].iloc[-1]),
                predictions=prediction_points,
                model_used="LSTM",
                trend=trend,
                risk_level="medium",
                recommendation=recommendation,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"LSTM prediction failed: {str(e)}")
            raise
    
    async def _prophet_predict(self, data: pd.DataFrame, symbol: str, days_ahead: int) -> PredictionResponse:
        """Prophet-based price prediction"""
        try:
            # Prepare data for Prophet
            df = data.reset_index()
            df = df.rename(columns={'Date': 'ds', 'Close': 'y'})
            
            # Create and fit model
            model = Prophet(
                daily_seasonality=False,
                weekly_seasonality=True,
                yearly_seasonality=True,
                changepoint_prior_scale=0.05
            )
            
            model.fit(df[['ds', 'y']])
            
            # Make future dataframe
            future = model.make_future_dataframe(periods=days_ahead)
            forecast = model.predict(future)
            
            # Extract predictions
            predictions = forecast.tail(days_ahead)
            
            # Create prediction points
            prediction_points = []
            for _, row in predictions.iterrows():
                confidence = 0.8  # Prophet provides good confidence intervals
                
                prediction_points.append(PredictionPoint(
                    date=row['ds'],
                    predicted_price=float(row['yhat']),
                    confidence_interval_lower=float(row['yhat_lower']),
                    confidence_interval_upper=float(row['yhat_upper']),
                    confidence_score=confidence
                ))
            
            # Calculate trend and recommendation
            trend = "bullish" if predictions['yhat'].iloc[-1] > data['Close'].iloc[-1] else "bearish"
            recommendation = self._generate_recommendation_signal(data, predictions['yhat'].values)
            
            return PredictionResponse(
                symbol=symbol,
                current_price=float(data['Close'].iloc[-1]),
                predictions=prediction_points,
                model_used="Prophet",
                trend=trend,
                risk_level="low",
                recommendation=recommendation,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Prophet prediction failed: {str(e)}")
            raise
    
    async def _random_forest_predict(self, data: pd.DataFrame, symbol: str, days_ahead: int) -> PredictionResponse:
        """Random Forest-based price prediction"""
        try:
            # Prepare features
            features = ['SMA_20', 'SMA_50', 'RSI', 'MACD', 'Volume', 'Price_Change']
            df = data[features + ['Close']].dropna()
            
            X = df[features]
            y = df['Close']
            
            # Split data
            train_size = int(len(X) * 0.8)
            X_train, X_test = X[:train_size], X[train_size:]
            y_train, y_test = y[:train_size], y[train_size:]
            
            # Train model
            model = RandomForestRegressor(n_estimators=100, random_state=42)
            model.fit(X_train, y_train)
            
            # Make predictions
            predictions = []
            last_features = X.iloc[-1].values.reshape(1, -1)
            
            for i in range(days_ahead):
                pred = model.predict(last_features)[0]
                predictions.append(pred)
                
                # Update features (simplified approach)
                last_features[0, -1] = (pred - y.iloc[-1]) / y.iloc[-1]  # Update price change
            
            # Create prediction points
            prediction_points = []
            start_date = data.index[-1] + timedelta(days=1)
            
            for i, pred_price in enumerate(predictions):
                confidence = max(0.7, 1.0 - (i * 0.015))
                
                prediction_points.append(PredictionPoint(
                    date=start_date + timedelta(days=i),
                    predicted_price=float(pred_price),
                    confidence_interval_lower=float(pred_price * 0.92),
                    confidence_interval_upper=float(pred_price * 1.08),
                    confidence_score=confidence
                ))
            
            # Calculate trend and recommendation
            trend = "bullish" if predictions[-1] > data['Close'].iloc[-1] else "bearish"
            recommendation = self._generate_recommendation_signal(data, predictions)
            
            return PredictionResponse(
                symbol=symbol,
                current_price=float(data['Close'].iloc[-1]),
                predictions=prediction_points,
                model_used="Random Forest",
                trend=trend,
                risk_level="medium",
                recommendation=recommendation,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Random Forest prediction failed: {str(e)}")
            raise
    
    async def _linear_regression_predict(self, data: pd.DataFrame, symbol: str, days_ahead: int) -> PredictionResponse:
        """Linear Regression-based price prediction"""
        try:
            # Simple linear regression on price trend
            df = data['Close'].dropna()
            X = np.arange(len(df)).reshape(-1, 1)
            y = df.values
            
            model = LinearRegression()
            model.fit(X, y)
            
            # Predict future prices
            future_X = np.arange(len(df), len(df) + days_ahead).reshape(-1, 1)
            predictions = model.predict(future_X)
            
            # Create prediction points
            prediction_points = []
            start_date = data.index[-1] + timedelta(days=1)
            
            for i, pred_price in enumerate(predictions):
                confidence = max(0.5, 0.8 - (i * 0.02))
                
                prediction_points.append(PredictionPoint(
                    date=start_date + timedelta(days=i),
                    predicted_price=float(pred_price),
                    confidence_interval_lower=float(pred_price * 0.9),
                    confidence_interval_upper=float(pred_price * 1.1),
                    confidence_score=confidence
                ))
            
            # Calculate trend and recommendation
            trend = "bullish" if predictions[-1] > data['Close'].iloc[-1] else "bearish"
            recommendation = self._generate_recommendation_signal(data, predictions)
            
            return PredictionResponse(
                symbol=symbol,
                current_price=float(data['Close'].iloc[-1]),
                predictions=prediction_points,
                model_used="Linear Regression",
                trend=trend,
                risk_level="high",
                recommendation=recommendation,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Linear regression prediction failed: {str(e)}")
            raise
    
    async def _ensemble_predict(self, data: pd.DataFrame, symbol: str, days_ahead: int) -> PredictionResponse:
        """Ensemble prediction combining multiple models"""
        try:
            # Get predictions from different models
            lstm_pred = await self._lstm_predict(data, symbol, days_ahead)
            prophet_pred = await self._prophet_predict(data, symbol, days_ahead)
            rf_pred = await self._random_forest_predict(data, symbol, days_ahead)
            
            # Combine predictions with weights
            weights = {'lstm': 0.4, 'prophet': 0.35, 'rf': 0.25}
            
            prediction_points = []
            for i in range(days_ahead):
                ensemble_price = (
                    weights['lstm'] * lstm_pred.predictions[i].predicted_price +
                    weights['prophet'] * prophet_pred.predictions[i].predicted_price +
                    weights['rf'] * rf_pred.predictions[i].predicted_price
                )
                
                ensemble_confidence = (
                    weights['lstm'] * lstm_pred.predictions[i].confidence_score +
                    weights['prophet'] * prophet_pred.predictions[i].confidence_score +
                    weights['rf'] * rf_pred.predictions[i].confidence_score
                )
                
                prediction_points.append(PredictionPoint(
                    date=lstm_pred.predictions[i].date,
                    predicted_price=ensemble_price,
                    confidence_interval_lower=ensemble_price * 0.93,
                    confidence_interval_upper=ensemble_price * 1.07,
                    confidence_score=ensemble_confidence
                ))
            
            # Determine overall trend
            trend_votes = [lstm_pred.trend, prophet_pred.trend, rf_pred.trend]
            trend = max(set(trend_votes), key=trend_votes.count)
            
            # Generate recommendation
            recommendation = self._generate_recommendation_signal(
                data, [p.predicted_price for p in prediction_points]
            )
            
            return PredictionResponse(
                symbol=symbol,
                current_price=float(data['Close'].iloc[-1]),
                predictions=prediction_points,
                model_used="Ensemble (LSTM + Prophet + RF)",
                trend=trend,
                risk_level="low",
                recommendation=recommendation,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Ensemble prediction failed: {str(e)}")
            raise
    
    def _create_sequences(self, data: np.ndarray, sequence_length: int):
        """Create sequences for LSTM training"""
        X, y = [], []
        for i in range(sequence_length, len(data)):
            X.append(data[i-sequence_length:i])
            y.append(data[i])
        return np.array(X).reshape(-1, sequence_length, 1), np.array(y)
    
    def _generate_recommendation_signal(self, data: pd.DataFrame, predictions: List[float]) -> str:
        """Generate buy/sell/hold recommendation"""
        current_price = data['Close'].iloc[-1]
        predicted_price = predictions[-1] if predictions else current_price
        
        # Calculate percentage change
        price_change = (predicted_price - current_price) / current_price
        
        # Get technical indicators
        rsi = data['RSI'].iloc[-1] if 'RSI' in data.columns else 50
        
        # Simple recommendation logic
        if price_change > 0.05 and rsi < 70:
            return "buy"
        elif price_change < -0.05 or rsi > 80:
            return "sell"
        else:
            return "hold"
    
    async def get_trending_predictions(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Get trending stock predictions"""
        # This would typically fetch from a database or cache
        # For now, return mock trending predictions
        trending_symbols = ['AAPL', 'GOOGL', 'MSFT', 'TSLA', 'AMZN', 'NVDA', 'META', 'NFLX']
        
        trending_predictions = []
        for symbol in trending_symbols[:limit]:
            try:
                prediction = await self.predict_price(symbol, 7, ModelType.ENSEMBLE)
                trending_predictions.append({
                    'symbol': symbol,
                    'current_price': prediction.current_price,
                    'predicted_price_7d': prediction.predictions[-1].predicted_price,
                    'trend': prediction.trend,
                    'recommendation': prediction.recommendation
                })
            except Exception as e:
                logger.warning(f"Failed to get prediction for {symbol}: {str(e)}")
                continue
        
        return trending_predictions
    
    async def generate_recommendation(self, symbol: str, technical_analysis: Any, 
                                    sentiment: Any, prediction: PredictionResponse) -> Dict[str, Any]:
        """Generate comprehensive stock recommendation"""
        # Combine all analysis factors
        factors = {
            'technical_score': self._calculate_technical_score(technical_analysis),
            'sentiment_score': sentiment.sentiment_scores[0].overall_sentiment if sentiment.sentiment_scores else 0,
            'prediction_score': self._calculate_prediction_score(prediction),
        }
        
        # Calculate overall score
        overall_score = (
            factors['technical_score'] * 0.4 +
            factors['sentiment_score'] * 0.3 +
            factors['prediction_score'] * 0.3
        )
        
        # Generate recommendation
        if overall_score > 0.6:
            recommendation = "Strong Buy"
        elif overall_score > 0.2:
            recommendation = "Buy"
        elif overall_score > -0.2:
            recommendation = "Hold"
        elif overall_score > -0.6:
            recommendation = "Sell"
        else:
            recommendation = "Strong Sell"
        
        return {
            'recommendation': recommendation,
            'overall_score': overall_score,
            'factors': factors,
            'confidence': abs(overall_score),
            'reasoning': self._generate_reasoning(factors, overall_score)
        }
    
    def _calculate_technical_score(self, technical_analysis: Any) -> float:
        """Calculate technical analysis score"""
        if not technical_analysis or not hasattr(technical_analysis, 'indicators'):
            return 0.0
        
        buy_signals = sum(1 for indicator in technical_analysis.indicators if indicator.signal == 'buy')
        sell_signals = sum(1 for indicator in technical_analysis.indicators if indicator.signal == 'sell')
        total_signals = len(technical_analysis.indicators)
        
        if total_signals == 0:
            return 0.0
        
        return (buy_signals - sell_signals) / total_signals
    
    def _calculate_prediction_score(self, prediction: PredictionResponse) -> float:
        """Calculate prediction score based on price movement and confidence"""
        current_price = prediction.current_price
        future_price = prediction.predictions[-1].predicted_price
        confidence = prediction.predictions[-1].confidence_score
        
        price_change = (future_price - current_price) / current_price
        return price_change * confidence
    
    def _generate_reasoning(self, factors: Dict[str, float], overall_score: float) -> str:
        """Generate human-readable reasoning for the recommendation"""
        reasoning_parts = []
        
        if factors['technical_score'] > 0.3:
            reasoning_parts.append("Technical indicators are bullish")
        elif factors['technical_score'] < -0.3:
            reasoning_parts.append("Technical indicators are bearish")
        
        if factors['sentiment_score'] > 0.3:
            reasoning_parts.append("Market sentiment is positive")
        elif factors['sentiment_score'] < -0.3:
            reasoning_parts.append("Market sentiment is negative")
        
        if factors['prediction_score'] > 0.1:
            reasoning_parts.append("AI models predict price appreciation")
        elif factors['prediction_score'] < -0.1:
            reasoning_parts.append("AI models predict price decline")
        
        if not reasoning_parts:
            reasoning_parts.append("Mixed signals across all factors")
        
        return ". ".join(reasoning_parts) + "."
