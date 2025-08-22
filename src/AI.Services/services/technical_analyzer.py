import numpy as np
import pandas as pd
import yfinance as yf
from datetime import datetime, timedelta
from typing import List, Dict, Any, Optional
import logging
import ta
from scipy import stats

from models.prediction_models import TechnicalAnalysisResponse, TechnicalIndicator

logger = logging.getLogger(__name__)

class TechnicalAnalyzer:
    def __init__(self):
        self.indicators_config = {
            'RSI': {'period': 14, 'overbought': 70, 'oversold': 30},
            'MACD': {'fast': 12, 'slow': 26, 'signal': 9},
            'BB': {'period': 20, 'std': 2},
            'SMA': {'periods': [20, 50, 200]},
            'EMA': {'periods': [12, 26]},
            'STOCH': {'k_period': 14, 'd_period': 3},
            'ADX': {'period': 14},
            'CCI': {'period': 20},
            'WILLIAMS': {'period': 14}
        }
    
    async def analyze(self, symbol: str, period: str, indicators: List[str]) -> TechnicalAnalysisResponse:
        """Perform comprehensive technical analysis"""
        try:
            # Fetch historical data
            data = await self._fetch_data(symbol, period)
            
            # Calculate requested indicators
            calculated_indicators = []
            for indicator_name in indicators:
                if indicator_name in self.indicators_config:
                    indicator_result = await self._calculate_indicator(data, indicator_name)
                    if indicator_result:
                        calculated_indicators.append(indicator_result)
            
            # Determine overall signal
            overall_signal, strength = self._determine_overall_signal(calculated_indicators)
            
            # Calculate support and resistance levels
            support_levels, resistance_levels = self._calculate_support_resistance(data)
            
            # Perform trend analysis
            trend_analysis = self._analyze_trend(data)
            
            return TechnicalAnalysisResponse(
                symbol=symbol,
                period=period,
                indicators=calculated_indicators,
                overall_signal=overall_signal,
                strength=strength,
                support_levels=support_levels,
                resistance_levels=resistance_levels,
                trend_analysis=trend_analysis,
                generated_at=datetime.utcnow()
            )
            
        except Exception as e:
            logger.error(f"Technical analysis failed for {symbol}: {str(e)}")
            raise
    
    async def _fetch_data(self, symbol: str, period: str) -> pd.DataFrame:
        """Fetch historical data for technical analysis"""
        try:
            ticker = yf.Ticker(symbol)
            data = ticker.history(period=period)
            
            if data.empty:
                raise ValueError(f"No data found for symbol {symbol}")
            
            return data
            
        except Exception as e:
            logger.error(f"Failed to fetch data for {symbol}: {str(e)}")
            raise
    
    async def _calculate_indicator(self, data: pd.DataFrame, indicator_name: str) -> Optional[TechnicalIndicator]:
        """Calculate a specific technical indicator"""
        try:
            if indicator_name == 'RSI':
                return self._calculate_rsi(data)
            elif indicator_name == 'MACD':
                return self._calculate_macd(data)
            elif indicator_name == 'BB':
                return self._calculate_bollinger_bands(data)
            elif indicator_name == 'SMA':
                return self._calculate_sma(data)
            elif indicator_name == 'EMA':
                return self._calculate_ema(data)
            elif indicator_name == 'STOCH':
                return self._calculate_stochastic(data)
            elif indicator_name == 'ADX':
                return self._calculate_adx(data)
            elif indicator_name == 'CCI':
                return self._calculate_cci(data)
            elif indicator_name == 'WILLIAMS':
                return self._calculate_williams_r(data)
            else:
                logger.warning(f"Unknown indicator: {indicator_name}")
                return None
                
        except Exception as e:
            logger.error(f"Failed to calculate {indicator_name}: {str(e)}")
            return None
    
    def _calculate_rsi(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate RSI indicator"""
        config = self.indicators_config['RSI']
        rsi = ta.momentum.rsi(data['Close'], window=config['period'])
        current_rsi = rsi.iloc[-1]
        
        # Determine signal
        if current_rsi > config['overbought']:
            signal = 'sell'
            description = f"RSI is overbought at {current_rsi:.1f}"
        elif current_rsi < config['oversold']:
            signal = 'buy'
            description = f"RSI is oversold at {current_rsi:.1f}"
        else:
            signal = 'neutral'
            description = f"RSI is neutral at {current_rsi:.1f}"
        
        return TechnicalIndicator(
            name='RSI',
            value=float(current_rsi),
            signal=signal,
            description=description
        )
    
    def _calculate_macd(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate MACD indicator"""
        config = self.indicators_config['MACD']
        
        macd_line = ta.trend.macd(data['Close'], window_fast=config['fast'], window_slow=config['slow'])
        macd_signal = ta.trend.macd_signal(data['Close'], window_fast=config['fast'], 
                                         window_slow=config['slow'], window_sign=config['signal'])
        macd_histogram = macd_line - macd_signal
        
        current_histogram = macd_histogram.iloc[-1]
        prev_histogram = macd_histogram.iloc[-2]
        
        # Determine signal based on histogram crossover
        if current_histogram > 0 and prev_histogram <= 0:
            signal = 'buy'
            description = "MACD histogram crossed above zero (bullish crossover)"
        elif current_histogram < 0 and prev_histogram >= 0:
            signal = 'sell'
            description = "MACD histogram crossed below zero (bearish crossover)"
        elif current_histogram > prev_histogram:
            signal = 'buy' if current_histogram > 0 else 'neutral'
            description = "MACD histogram is increasing"
        else:
            signal = 'sell' if current_histogram < 0 else 'neutral'
            description = "MACD histogram is decreasing"
        
        return TechnicalIndicator(
            name='MACD',
            value=float(current_histogram),
            signal=signal,
            description=description
        )
    
    def _calculate_bollinger_bands(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Bollinger Bands indicator"""
        config = self.indicators_config['BB']
        
        bb = ta.volatility.BollingerBands(data['Close'], window=config['period'], window_dev=config['std'])
        upper_band = bb.bollinger_hband()
        lower_band = bb.bollinger_lband()
        middle_band = bb.bollinger_mavg()
        
        current_price = data['Close'].iloc[-1]
        current_upper = upper_band.iloc[-1]
        current_lower = lower_band.iloc[-1]
        current_middle = middle_band.iloc[-1]
        
        # Calculate position within bands
        band_position = (current_price - current_lower) / (current_upper - current_lower)
        
        # Determine signal
        if band_position > 0.8:
            signal = 'sell'
            description = f"Price near upper Bollinger Band (position: {band_position:.2f})"
        elif band_position < 0.2:
            signal = 'buy'
            description = f"Price near lower Bollinger Band (position: {band_position:.2f})"
        else:
            signal = 'neutral'
            description = f"Price within Bollinger Bands (position: {band_position:.2f})"
        
        return TechnicalIndicator(
            name='Bollinger Bands',
            value=float(band_position),
            signal=signal,
            description=description
        )
    
    def _calculate_sma(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Simple Moving Average indicator"""
        config = self.indicators_config['SMA']
        current_price = data['Close'].iloc[-1]
        
        # Calculate multiple SMAs
        sma_20 = ta.trend.sma_indicator(data['Close'], window=20).iloc[-1]
        sma_50 = ta.trend.sma_indicator(data['Close'], window=50).iloc[-1]
        
        # Determine signal based on price vs SMA and SMA crossovers
        if current_price > sma_20 > sma_50:
            signal = 'buy'
            description = f"Price above SMA20 ({sma_20:.2f}) and SMA50 ({sma_50:.2f})"
        elif current_price < sma_20 < sma_50:
            signal = 'sell'
            description = f"Price below SMA20 ({sma_20:.2f}) and SMA50 ({sma_50:.2f})"
        else:
            signal = 'neutral'
            description = f"Mixed SMA signals - Price: {current_price:.2f}, SMA20: {sma_20:.2f}, SMA50: {sma_50:.2f}"
        
        return TechnicalIndicator(
            name='SMA',
            value=float(current_price / sma_20),  # Price to SMA20 ratio
            signal=signal,
            description=description
        )
    
    def _calculate_ema(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Exponential Moving Average indicator"""
        config = self.indicators_config['EMA']
        current_price = data['Close'].iloc[-1]
        
        ema_12 = ta.trend.ema_indicator(data['Close'], window=12).iloc[-1]
        ema_26 = ta.trend.ema_indicator(data['Close'], window=26).iloc[-1]
        
        # Determine signal based on EMA crossover
        if ema_12 > ema_26 and current_price > ema_12:
            signal = 'buy'
            description = f"EMA12 ({ema_12:.2f}) above EMA26 ({ema_26:.2f}) with price above EMA12"
        elif ema_12 < ema_26 and current_price < ema_12:
            signal = 'sell'
            description = f"EMA12 ({ema_12:.2f}) below EMA26 ({ema_26:.2f}) with price below EMA12"
        else:
            signal = 'neutral'
            description = f"Mixed EMA signals - EMA12: {ema_12:.2f}, EMA26: {ema_26:.2f}"
        
        return TechnicalIndicator(
            name='EMA',
            value=float(ema_12 / ema_26),  # EMA12 to EMA26 ratio
            signal=signal,
            description=description
        )
    
    def _calculate_stochastic(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Stochastic Oscillator"""
        config = self.indicators_config['STOCH']
        
        stoch_k = ta.momentum.stoch(data['High'], data['Low'], data['Close'], 
                                  window=config['k_period'], smooth_window=config['d_period'])
        stoch_d = ta.momentum.stoch_signal(data['High'], data['Low'], data['Close'],
                                         window=config['k_period'], smooth_window=config['d_period'])
        
        current_k = stoch_k.iloc[-1]
        current_d = stoch_d.iloc[-1]
        
        # Determine signal
        if current_k < 20 and current_d < 20:
            signal = 'buy'
            description = f"Stochastic oversold (%K: {current_k:.1f}, %D: {current_d:.1f})"
        elif current_k > 80 and current_d > 80:
            signal = 'sell'
            description = f"Stochastic overbought (%K: {current_k:.1f}, %D: {current_d:.1f})"
        else:
            signal = 'neutral'
            description = f"Stochastic neutral (%K: {current_k:.1f}, %D: {current_d:.1f})"
        
        return TechnicalIndicator(
            name='Stochastic',
            value=float(current_k),
            signal=signal,
            description=description
        )
    
    def _calculate_adx(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Average Directional Index"""
        config = self.indicators_config['ADX']
        
        adx = ta.trend.adx(data['High'], data['Low'], data['Close'], window=config['period'])
        current_adx = adx.iloc[-1]
        
        # Determine signal based on trend strength
        if current_adx > 25:
            signal = 'buy'  # Strong trend (could be up or down, need +DI/-DI for direction)
            description = f"Strong trend detected (ADX: {current_adx:.1f})"
        elif current_adx < 20:
            signal = 'neutral'
            description = f"Weak trend/sideways market (ADX: {current_adx:.1f})"
        else:
            signal = 'neutral'
            description = f"Moderate trend strength (ADX: {current_adx:.1f})"
        
        return TechnicalIndicator(
            name='ADX',
            value=float(current_adx),
            signal=signal,
            description=description
        )
    
    def _calculate_cci(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Commodity Channel Index"""
        config = self.indicators_config['CCI']
        
        cci = ta.trend.cci(data['High'], data['Low'], data['Close'], window=config['period'])
        current_cci = cci.iloc[-1]
        
        # Determine signal
        if current_cci > 100:
            signal = 'sell'
            description = f"CCI overbought ({current_cci:.1f})"
        elif current_cci < -100:
            signal = 'buy'
            description = f"CCI oversold ({current_cci:.1f})"
        else:
            signal = 'neutral'
            description = f"CCI neutral ({current_cci:.1f})"
        
        return TechnicalIndicator(
            name='CCI',
            value=float(current_cci),
            signal=signal,
            description=description
        )
    
    def _calculate_williams_r(self, data: pd.DataFrame) -> TechnicalIndicator:
        """Calculate Williams %R"""
        config = self.indicators_config['WILLIAMS']
        
        williams_r = ta.momentum.williams_r(data['High'], data['Low'], data['Close'], 
                                          lbp=config['period'])
        current_wr = williams_r.iloc[-1]
        
        # Determine signal
        if current_wr > -20:
            signal = 'sell'
            description = f"Williams %R overbought ({current_wr:.1f})"
        elif current_wr < -80:
            signal = 'buy'
            description = f"Williams %R oversold ({current_wr:.1f})"
        else:
            signal = 'neutral'
            description = f"Williams %R neutral ({current_wr:.1f})"
        
        return TechnicalIndicator(
            name='Williams %R',
            value=float(current_wr),
            signal=signal,
            description=description
        )
    
    def _determine_overall_signal(self, indicators: List[TechnicalIndicator]) -> tuple[str, float]:
        """Determine overall signal from all indicators"""
        if not indicators:
            return 'neutral', 0.0
        
        buy_count = sum(1 for ind in indicators if ind.signal == 'buy')
        sell_count = sum(1 for ind in indicators if ind.signal == 'sell')
        total_count = len(indicators)
        
        buy_ratio = buy_count / total_count
        sell_ratio = sell_count / total_count
        
        # Determine overall signal
        if buy_ratio >= 0.6:
            signal = 'buy'
            strength = buy_ratio
        elif sell_ratio >= 0.6:
            signal = 'sell'
            strength = sell_ratio
        else:
            signal = 'neutral'
            strength = max(buy_ratio, sell_ratio)
        
        return signal, strength
    
    def _calculate_support_resistance(self, data: pd.DataFrame, window: int = 20) -> tuple[List[float], List[float]]:
        """Calculate support and resistance levels"""
        try:
            # Use local minima and maxima to identify support and resistance
            highs = data['High'].rolling(window=window, center=True).max()
            lows = data['Low'].rolling(window=window, center=True).min()
            
            # Find resistance levels (local maxima)
            resistance_levels = []
            for i in range(window, len(data) - window):
                if data['High'].iloc[i] == highs.iloc[i]:
                    resistance_levels.append(float(data['High'].iloc[i]))
            
            # Find support levels (local minima)
            support_levels = []
            for i in range(window, len(data) - window):
                if data['Low'].iloc[i] == lows.iloc[i]:
                    support_levels.append(float(data['Low'].iloc[i]))
            
            # Remove duplicates and sort
            resistance_levels = sorted(list(set(resistance_levels)), reverse=True)[:5]
            support_levels = sorted(list(set(support_levels)))[:5]
            
            return support_levels, resistance_levels
            
        except Exception as e:
            logger.error(f"Failed to calculate support/resistance: {str(e)}")
            return [], []
    
    def _analyze_trend(self, data: pd.DataFrame) -> Dict[str, Any]:
        """Analyze price trend"""
        try:
            # Calculate trend using linear regression
            prices = data['Close'].values
            x = np.arange(len(prices))
            
            slope, intercept, r_value, p_value, std_err = stats.linregress(x, prices)
            
            # Determine trend direction and strength
            if slope > 0:
                trend_direction = 'uptrend'
            elif slope < 0:
                trend_direction = 'downtrend'
            else:
                trend_direction = 'sideways'
            
            # Calculate trend strength based on R-squared
            trend_strength = abs(r_value) ** 2
            
            # Calculate recent momentum (last 10 days vs previous 10 days)
            if len(data) >= 20:
                recent_avg = data['Close'].tail(10).mean()
                previous_avg = data['Close'].iloc[-20:-10].mean()
                momentum = (recent_avg - previous_avg) / previous_avg
            else:
                momentum = 0.0
            
            return {
                'direction': trend_direction,
                'strength': float(trend_strength),
                'slope': float(slope),
                'r_squared': float(r_value ** 2),
                'momentum': float(momentum),
                'duration_days': len(data)
            }
            
        except Exception as e:
            logger.error(f"Failed to analyze trend: {str(e)}")
            return {
                'direction': 'unknown',
                'strength': 0.0,
                'slope': 0.0,
                'r_squared': 0.0,
                'momentum': 0.0,
                'duration_days': 0
            }
